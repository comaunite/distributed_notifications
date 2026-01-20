using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Integrations.RabbitMQ;
using Microsoft.Extensions.Logging;

namespace NotificationWorker.Email.Handlers;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
internal sealed class EmailMessageHandler(ILogger<EmailMessageHandler> logger) : IMessageHandler
{
    public ValueTask<(bool success, string? error)> ProcessAsync(ReadOnlyMemory<byte> body, string? correlationId, CancellationToken cancellationToken)
    {
        var message = JsonSerializer.Deserialize(body.Span,
            Integrations.RabbitMQ.Serialization.NotificationSerializationContext.Default.NotifyEmail);

        if (message == null)
        {
            return new ValueTask<(bool success, string? error)>((false, "Failed to deserialize Push notification message"));
        }

        // TODO: Handle deduplication

        // TODO: This is debug only, technically sending logic goes here
        logger.LogInformation("Processing Push Notification: {NotificationId} to {EmailAddress}",
            message.NotificationId, message.EmailAddress);

        return new ValueTask<(bool success, string? error)>((true, null));
    }
}