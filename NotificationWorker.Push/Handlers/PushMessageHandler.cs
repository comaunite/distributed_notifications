using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Integrations.RabbitMQ;
using Microsoft.Extensions.Logging;
using Constants = Integrations.RabbitMQ.Constants;

namespace NotificationWorker.Push.Handlers;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
internal sealed class PushMessageHandler(ILogger<PushMessageHandler> logger) : IMessageHandler
{
    public string QueueName => Constants.Queues.PushWorker;

    public ValueTask<(bool success, string? error)> ProcessAsync(ReadOnlyMemory<byte> body, string? correlationId, CancellationToken cancellationToken)
    {
        var message = JsonSerializer.Deserialize(body.Span,
            Integrations.RabbitMQ.Serialization.NotificationSerializationContext.Default.NotifyPush);

        if (message == null)
        {
            return new ValueTask<(bool success, string? error)>((false, "Failed to deserialize Push notification message"));
        }

        // TODO: Handle deduplication

        // TODO: This is debug only, technically sending logic goes here
        logger.LogInformation("Processing Push Notification: {NotificationId} to {DeviceToken}",
            message.NotificationId, message.DeviceToken);

        return new ValueTask<(bool success, string? error)>((true, null));
    }
}