using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Threading.Channels;
using Common.Enums;
using Integrations.RabbitMQ;
using Integrations.RabbitMQ.Models;
using Integrations.RabbitMQ.Models.Base;
using Integrations.RabbitMQ.Serialization;
using Microsoft.Extensions.Logging;
using NotificationOrchestrator.Models;
using Persistence.Models;
using Persistence.Stores;
using RabbitMQ.Client;
using Constants = Integrations.RabbitMQ.Constants;

namespace NotificationOrchestrator.Services;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
internal sealed class OrchestrationHandler(INotificationStore store, IRabbitMqPublisher publisher, RabbitMqPublisherChannelPool publisherChannelPool,
    ILogger<OrchestrationHandler> logger)
    : IMessageHandler
{
    private static readonly Dictionary<DeliveryChannel, string> routingKeys = new()
    {
        [DeliveryChannel.Email] = Constants.RoutingKeys.SendEmail,
        [DeliveryChannel.Sms] = Constants.RoutingKeys.SendSms,
        [DeliveryChannel.Push] = Constants.RoutingKeys.SendPush
    };

    [SuppressMessage("Design", "CA1031:Do not catch general exception types")]
    public async Task<(bool success, string? error, bool canRetry)> ProcessAsync(ReadOnlyMemory<byte> body, IReadOnlyBasicProperties props,
        CancellationToken cancellationToken)
    {
        var message = JsonSerializer.Deserialize(body.Span,
            NotificationSerializationContext.Default.BaseNotification);

        if (message == null)
        {
            return (false, $"Failed to deserialize notification message. CorrelationId: {props.CorrelationId}", false);
        }

        try
        {
            // Logging for debugging purposes only, in production should probably avoid logging every message
            logger.LogInformation("Orchestrating notification with Correlation ID '{CorrelationId}' and Notification ID '{NotificationId}'",
                props.CorrelationId, message.NotificationId);

            return await ProcessAndFanOutAsync(message, props, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing notification with Correlation ID '{CorrelationId}' and Notification ID '{NotificationId}'",
                props.CorrelationId, message.NotificationId);

            return (false, ex.Message, true);
        }
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types")]
    private async Task<(bool success, string? error, bool canRetry)> ProcessAndFanOutAsync(BaseNotification message, IReadOnlyBasicProperties props,
        CancellationToken cancellationToken)
    {
        var result = new ResultModel();

        var timer = Stopwatch.StartNew();

        var templateCache = InitializeTemplateCache(message);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        var channel = Channel.CreateBounded<NotificationRecipient>(new BoundedChannelOptions(1000)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = false,
            SingleWriter = true
        });

        try
        {
            var producer = GetProducerTask(channel, message, props.CorrelationId, cts.Token);
            var consumers = GetConsumerTasks(channel, message, props.CorrelationId, templateCache, result, timer, cts.Token);

            await Task.WhenAll(consumers.Append(producer));
        }
        catch (Exception ex)
        {
            await cts.CancelAsync();

            logger.LogError(ex, "Error during fan-out for {CorrelationId}", props.CorrelationId);

            return (false, ex.Message, true);
        }

        timer.Stop();

        if (result.FailureCount > 0)
        {
            var successCount = result.TotalCount - result.FailureCount;
            var failureRate = result.TotalCount > 0
                ? (double)result.FailureCount / (result.TotalCount == 0 ? 1 : result.TotalCount)
                : 1;

            logger.LogWarning(
                "Notification {CorrelationId} completed with {SuccessCount}/{TotalCount} successes ({FailureRate:P1} failure rate) over {ElapsedMilliseconds} ms",
                props.CorrelationId, successCount, result.TotalCount, failureRate, timer.ElapsedMilliseconds);

            // Decide if partial failure should fail the entire operation
            if (failureRate > 0.5)
            {
                return (false, $"High failure rate: {result.FailureCount}/{result.TotalCount} recipients failed", false);
            }

            // Failures are likely due to some bug or infrastructure issue
            // There are ways to optimize this further, if we have a partial success.
            // That would help avoid retrying for those messages that were queued successfully (e.g. problem with a specific channel)
            // Can be checked via deduplication id before publishing, but for now we keep it simple.
            return (false, $"Partial failure: {result.FailureCount}/{result.TotalCount} recipients failed", true);
        }

        logger.LogInformation("Successfully orchestrated notification {CorrelationId} to {RecipientCount} recipients over {ElapsedMilliseconds} ms",
            props.CorrelationId, result.TotalCount, timer.ElapsedMilliseconds);

        return (true, null, false);
    }

    private Task GetProducerTask(Channel<NotificationRecipient> channel, BaseNotification message, string? correlationId, CancellationToken cancellationToken)
    {
        var producer = Task.Run(async () =>
        {
            try
            {
                await foreach (var recipient in store.GetNotificationRecipientsAsync(message.Type, cancellationToken))
                {
                    await channel.Writer.WriteAsync(recipient, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error producing recipients for notification {CorrelationId}", correlationId);

                throw;
            }
            finally
            {
                channel.Writer.Complete();
            }
        }, cancellationToken);
        return producer;
    }

    private IEnumerable<Task> GetConsumerTasks(Channel<NotificationRecipient> channel, BaseNotification message, string? correlationId,
        Dictionary<DeliveryChannel, byte[]> templateCache, ResultModel result, Stopwatch timer, CancellationToken cancellationToken)
    {
        var consumers = Enumerable.Range(0, 16).Select(async _ =>
        {
            await using var rabbitMqChannel = await publisherChannelPool.RentChannelAsync(cancellationToken);

            var headers = new Dictionary<string, object?>(2);
            var basicProperties = new BasicProperties
            {
                Persistent = false,
                ContentType = "application/json",
                CorrelationId = correlationId,
            };

            await foreach (var recipient in channel.Reader.ReadAllAsync(cancellationToken))
            {
                if (templateCache.TryGetValue(recipient.DeliveryChannel, out var template))
                {
                    // var deduplicationId = GuidHelper.CreateDeterministic(
                    //     message.NotificationId,
                    //     recipient.UserId,
                    //     (int)recipient.DeliveryChannel);

                    headers[Constants.HeaderKeys.DeliveryAddress] = recipient.DeliveryAddress;
                    headers[Constants.HeaderKeys.DeduplicationId] = $"{message.NotificationId:N}-{recipient.UserId:N}-{(int)recipient.DeliveryChannel}";

                    basicProperties.Headers = headers;

                    var (success, error) = await publisher.PublishAsync(
                        rabbitMqChannel.Channel,
                        template,
                        basicProperties,
                        routingKeys[recipient.DeliveryChannel],
                        false,
                        cancellationToken);

                    if (!success)
                    {
                        logger.LogError("Failed to publish {CorrelationId} to {Channel} for {UserId}: {Error}",
                            correlationId, recipient.DeliveryChannel, recipient.UserId, error);

                        result.IncrementFailure();
                    }
                }
                else
                {
                    logger.LogWarning("Failed to map notification {CorrelationId} for user {UserId} and channel {Channel}",
                        correlationId, recipient.UserId, recipient.DeliveryChannel);

                    result.IncrementFailure();
                }

                result.IncrementTotal();

                if (result.TotalCount % 100000 == 0)
                {
                    logger.LogInformation("Orchestrated notification {CorrelationId} to {Count} recipients over {ElapsedMilliseconds} ms",
                        correlationId, result.TotalCount, timer.ElapsedMilliseconds);
                }
            }
        });

        return consumers;
    }

    private static Dictionary<DeliveryChannel, byte[]> InitializeTemplateCache(BaseNotification message)
    {
        var notificationTemplateDictionary = new Dictionary<DeliveryChannel, byte[]>();

        notificationTemplateDictionary.TryAdd(DeliveryChannel.Email, JsonSerializer.SerializeToUtf8Bytes(
            new NotifyEmail(message),
            typeof(NotifyEmail),
            NotificationSerializationContext.Default));

        notificationTemplateDictionary.TryAdd(DeliveryChannel.Sms, JsonSerializer.SerializeToUtf8Bytes(
            new NotifySms(message),
            typeof(NotifySms),
            NotificationSerializationContext.Default));

        notificationTemplateDictionary.TryAdd(DeliveryChannel.Push, JsonSerializer.SerializeToUtf8Bytes(
            new NotifyPush(message),
            typeof(NotifyPush),
            NotificationSerializationContext.Default));

        return notificationTemplateDictionary;
    }
}