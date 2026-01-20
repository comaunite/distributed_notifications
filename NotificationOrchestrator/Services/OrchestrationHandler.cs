using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Common;
using Common.Enums;
using Integrations.RabbitMQ;
using Integrations.RabbitMQ.Models;
using Integrations.RabbitMQ.Models.Base;
using Microsoft.Extensions.Logging;
using NotificationOrchestrator.Models;
using Constants = Integrations.RabbitMQ.Constants;

namespace NotificationOrchestrator.Services;

internal sealed class OrchestrationHandler(IRabbitMqPublisher publisher, ILogger<OrchestrationHandler> logger) : IMessageHandler
{
    public string QueueName => Constants.Queues.Orchestrator;

    // ValueTask is suboptimal in this case, since task is 99% will complete asynchronously.
    // Though for now keeping it for uniformity with other handlers,
    // where hotpath synchronous completion is possible.
    [SuppressMessage("Design", "CA1031:Do not catch general exception types")]
    public async ValueTask<(bool success, string? error)> ProcessAsync(ReadOnlyMemory<byte> body, string? correlationId, CancellationToken cancellationToken)
    {
        var message = JsonSerializer.Deserialize(body.Span,
            Integrations.RabbitMQ.Serialization.NotificationSerializationContext.Default.BaseNotification);

        if (message == null)
        {
            return (false, "Failed to deserialize notification message");
        }

        try
        {
            // TODO: Logging for debugging purposes only
            logger.LogInformation("Orchestrating notification with ID '{NotificationId}'", message.NotificationId);

            await ProcessAndFanOutAsync(message, correlationId);

            return (true, null);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing notification with ID '{NotificationId}'", message.NotificationId);

            return (false, ex.Message);
        }
    }

    private async Task ProcessAndFanOutAsync(BaseNotification message, string? correlationId)
    {
        // Get the users subscribed to this notification type

        // TODO: 1) Move to DB
        // TODO: 2) Implement Redis cache
        var users = new[]
        {
            new UserNotificationPreferences(
                Guid.NewGuid(),
                "bob.miller@example.com",
                "+15551234567",
                "device_token_123",
                [ DeliveryChannel.Email, DeliveryChannel.Sms ]
            ),
            new UserNotificationPreferences(
                Guid.NewGuid(),
                "alice.smith@example.com",
                "+15559876543",
                "device_token_123",
                [ DeliveryChannel.Email, DeliveryChannel.Push ]
            ),
            new UserNotificationPreferences(
                Guid.NewGuid(),
                "charlie.jones@example.com",
                "+15551122334",
                "device_token_123",
                [ DeliveryChannel.Email, DeliveryChannel.Sms, DeliveryChannel.Push ]
            ),
            new UserNotificationPreferences(
                Guid.NewGuid(),
                "guy.simmons@example.com",
                "+15555555555",
                "device_token_123",
                [ DeliveryChannel.Email ]
            ),
        };

        // TODO: Needs validation that delivery destinations are specified for each channel preferred

        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = 50,
            CancellationToken = CancellationToken.None
        };

        await Parallel.ForEachAsync(users, options, async (user, cancellationToken) =>
        {
            // Process each channel for the user
            foreach (var channelType in user.PreferredChannels)
            {
                var deduplicationId = GuidHelper.CreateDeterministic(
                    message.NotificationId,
                    user.UserId,
                    (int)channelType);

                BaseNotification? notification = channelType switch
                {
                    DeliveryChannel.Email => new NotifyEmail
                    {
                        NotificationId = message.NotificationId,
                        DeduplicationId = deduplicationId,
                        EmailAddress = user.Email!,
                        Type = NotificationType.NewPost,
                        CreatedAt = DateTimeOffset.UtcNow
                    },
                    DeliveryChannel.Sms => new NotifySms
                    {
                        NotificationId = message.NotificationId,
                        DeduplicationId = deduplicationId,
                        PhoneNumber = user.PhoneNumber!,
                        Type = NotificationType.NewPost,
                        CreatedAt = DateTimeOffset.UtcNow
                    },
                    DeliveryChannel.Push => new NotifyPush
                    {
                        NotificationId = message.NotificationId,
                        DeduplicationId = deduplicationId,
                        DeviceToken = user.DeviceToken!,
                        Type = NotificationType.NewPost,
                        CreatedAt = DateTimeOffset.UtcNow
                    },
                    _ => null
                };

                if (notification != null)
                {
                    var (success, error) = await publisher.PublishAsync(notification, correlationId, cancellationToken);

                    if (!success)
                    {
                        logger.LogError("Failed to publish to {Channel}: {Error}", channelType, error);
                    }
                }
                else
                {
                    logger.LogError("Encountered unsupported delivery channel: {Channel} for NotificationId '{NotificationId}' and UserId '{UserId}'",
                        channelType, message.NotificationId, user.UserId);
                }
            }
        });

        // TODO: Handle partial failures?
    }
}