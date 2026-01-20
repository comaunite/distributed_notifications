using System.Diagnostics.CodeAnalysis;
using Integrations.RabbitMQ.Models;
using Integrations.RabbitMQ.Models.Base;
using Integrations.RabbitMQ.Serialization;
using RabbitMQ.Client;

namespace Integrations.RabbitMQ;

public interface IRabbitMqPublisher
{
    Task<(bool success, string? errorMessage)> PublishAsync<T>(T message, string? correlationId, CancellationToken cancellationToken)
        where T : BaseNotification;
}

public class RabbitMqPublisher(RabbitMqPublisherChannelPool publisherChannelPool) : IRabbitMqPublisher
{
    private static readonly Dictionary<Type, string> routingKeyMap = new()
    {
        [typeof(NotificationCreated)] = Constants.RoutingKeys.NotificationCreated,
        [typeof(NotifyEmail)] = Constants.RoutingKeys.SendEmail,
        [typeof(NotifySms)] = Constants.RoutingKeys.SendSms,
        [typeof(NotifyPush)] = Constants.RoutingKeys.SendPush
    };

    [SuppressMessage("Design", "CA1031:Do not catch general exception types")]
    public async Task<(bool success, string? errorMessage)> PublishAsync<T>(T message, string? correlationId, CancellationToken cancellationToken)
        where T : BaseNotification
    {
        try
        {
            ArgumentNullException.ThrowIfNull(message);

            await using var channel = await publisherChannelPool.RentChannelAsync(cancellationToken);

            var body = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(
                message,
                message.GetType(),
                NotificationSerializationContext.Default);

            var props = new BasicProperties
            {
                Persistent = false, // For demo purposes I don't want to persist messages
                ContentType = "application/json",
                MessageId = message.NotificationId.ToString(),
                CorrelationId = correlationId,
                Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds())
            };

            await channel.Channel.BasicPublishAsync(
                exchange: Constants.Exchange,
                routingKey: GetRoutingKeyForMessageType(message.GetType()),
                mandatory: true,
                basicProperties: props,
                body: body,
                cancellationToken: cancellationToken);

            return (true, null);
        }
        catch (Exception ex)
        {
            // According to docs, BasicPublishAsync will throw if the message could not be routed
            return (false, ex.Message);
        }
    }

    private static string GetRoutingKeyForMessageType(Type messageType)
    {
        if (routingKeyMap.TryGetValue(messageType, out var routingKey))
        {
            return routingKey;
        }

        throw new InvalidOperationException($"No routing key defined for message type {messageType.FullName}");
    }
}