using Integrations.RabbitMQ.Models;
using Integrations.RabbitMQ.Models.Base;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace Integrations.RabbitMQ;

public class RabbitMqPublisher(IChannel channel, string exchangeName)
{
    public async Task<(bool success, string? errorMessage)> PublishAsync<T>(T message, CancellationToken ct)
        where T : BaseNotification
    {
        ArgumentNullException.ThrowIfNull(message);

        try
        {
            var body = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(message);

            var props = new BasicProperties
            {
                Persistent = false, // For demo purposes I don't want to persist messages
                ContentType = "application/json",
                MessageId = message.NotificationId.ToString(),
                Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds())
            };

            await channel.BasicPublishAsync(
                exchange: exchangeName,
                routingKey: GetRoutingKeyForMessageType(typeof(T)),
                mandatory: true,
                basicProperties: props,
                body: body,
                cancellationToken: ct);

            return (true, null);
        }
        catch (PublishException ex)
        {
            return (false, ex.Message);
        }
    }

    private static string GetRoutingKeyForMessageType(Type messageType)
    {
        if (messageType == typeof(NotificationCreated))
        {
            return Constants.RoutingKeys.NotificationCreated;
        }

        throw new InvalidOperationException($"No routing key defined for message type {messageType.FullName}");
    }
}