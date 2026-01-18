using System.Diagnostics.CodeAnalysis;
using Integrations.RabbitMQ.Models;
using Integrations.RabbitMQ.Models.Base;
using Integrations.RabbitMQ.Serialization;
using RabbitMQ.Client;

namespace Integrations.RabbitMQ.Publishers;

public class RabbitMqPublisher(IChannel channel, string exchangeName)
{
    [SuppressMessage("Design", "CA1031:Do not catch general exception types")]
    public async Task<(bool success, string? errorMessage)> PublishAsync<T>(T message, CancellationToken cancellationToken)
        where T : BaseNotification
    {
        try
        {
            ArgumentNullException.ThrowIfNull(message);

            var body = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(
                message,
                message.GetType(),
                NotificationSerializationContext.Default);

            var props = new BasicProperties
            {
                Persistent = false, // For demo purposes I don't want to persist messages
                ContentType = "application/json",
                MessageId = message.NotificationId.ToString(),
                Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds())
            };

            await channel.BasicPublishAsync(
                exchange: exchangeName,
                routingKey: GetRoutingKeyForMessageType(message.GetType()),
                mandatory: true,
                basicProperties: props,
                body: body,
                cancellationToken: cancellationToken);

            // TODO: Check if publish was successful

            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    private static readonly Dictionary<Type, string> routingKeyMap = new()
    {
        [typeof(NotificationCreated)] = Constants.RoutingKeys.NotificationCreated,
        [typeof(NotifyEmail)] = Constants.RoutingKeys.SendEmail,
        [typeof(NotifySms)] = Constants.RoutingKeys.SendSms,
        [typeof(NotifyPush)] = Constants.RoutingKeys.SendPush
    };

    private static string GetRoutingKeyForMessageType(Type messageType)
    {
        if (routingKeyMap.TryGetValue(messageType, out var routingKey))
        {
            return routingKey;
        }

        throw new InvalidOperationException($"No routing key defined for message type {messageType.FullName}");
    }
}