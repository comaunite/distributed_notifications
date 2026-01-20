using System.Text.Json;
using Integrations.RabbitMQ;
using Microsoft.Extensions.Logging;
using Constants = Integrations.RabbitMQ.Constants;

namespace NotificationWorker.SMS.Handlers;

internal sealed class SmsMessageHandler(ILogger<SmsMessageHandler> logger) : IMessageHandler
{
    public string QueueName => Constants.Queues.SmsWorker;

    public ValueTask<(bool success, string? error)> ProcessAsync(ReadOnlyMemory<byte> body, string? correlationId, CancellationToken cancellationToken)
    {
        var message = JsonSerializer.Deserialize(body.Span,
            Integrations.RabbitMQ.Serialization.NotificationSerializationContext.Default.NotifySms);

        if (message == null)
        {
            return new ValueTask<(bool success, string? error)>((false, "Failed to deserialize SMS notification message"));
        }

        // TODO: Handle deduplication

        // TODO: This is debug only, technically sending logic goes here
        logger.LogInformation("Processing SMS Notification: {NotificationId} to {PhoneNumber}",
            message.NotificationId, message.PhoneNumber);

        return new ValueTask<(bool success, string? error)>((true, null));
    }
}