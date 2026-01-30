using System.Diagnostics.CodeAnalysis;
using RabbitMQ.Client;

namespace Integrations.RabbitMQ;

public interface IRabbitMqPublisher
{
    Task<(bool success, string? errorMessage)> PublishAsync(byte[] body, Dictionary<string, object?>? headers, string? correlationId,
        string routingKey, bool requireConfirmation, CancellationToken cancellationToken);
}

public class RabbitMqPublisher(RabbitMqPublisherChannelPool publisherChannelPool) : IRabbitMqPublisher
{
    [SuppressMessage("Design", "CA1031:Do not catch general exception types")]
    public async Task<(bool success, string? errorMessage)> PublishAsync(byte[] body, Dictionary<string, object?>? headers,
        string? correlationId, string routingKey, bool requireConfirmation, CancellationToken cancellationToken)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(body);

            await using var channel = await publisherChannelPool.RentChannelAsync(cancellationToken);

            var props = new BasicProperties
            {
                Persistent = false, // For demo purposes I don't want to persist messages
                ContentType = "application/json",
                CorrelationId = correlationId,
                Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
                Headers = headers
            };

            await channel.Channel.BasicPublishAsync(
                exchange: Constants.Exchange,
                routingKey: routingKey,
                mandatory: requireConfirmation,
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
}