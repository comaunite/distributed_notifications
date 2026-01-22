using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Integrations.RabbitMQ;
using Microsoft.Extensions.Logging;
using Persistence.Stores;

namespace NotificationWorker.Email.Handlers;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
internal sealed class EmailMessageHandler(IDeduplicationStore deduplicationStore, ILogger<EmailMessageHandler> logger) : IMessageHandler
{
    public async Task<(bool success, string? error, bool canRetry)> ProcessAsync(ReadOnlyMemory<byte> body, string? correlationId, CancellationToken cancellationToken)
    {
        var message = JsonSerializer.Deserialize(body.Span,
            Integrations.RabbitMQ.Serialization.NotificationSerializationContext.Default.NotifyEmail);

        if (message == null)
        {
            return (false, "Failed to deserialize Email notification message", false);
        }

        var deduplicationId = message.DeduplicationId.ToString();

        if (await deduplicationStore.IsDuplicateAsync(deduplicationId))
        {
            logger.LogInformation("Duplicate Email Notification detected: {NotificationId}, skipping processing.", message.NotificationId);

            // Expecting to Ack the message
            return (true, null, false);
        }

        logger.LogInformation("Processing Email Notification: {NotificationId} to {EmailAddress}", message.NotificationId, message.DeliveryAddress);

        await deduplicationStore.MarkAsProcessedAsync(deduplicationId);

        return (true, null, false);
    }
}