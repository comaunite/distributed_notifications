using System.Diagnostics.CodeAnalysis;
using RabbitMQ.Client;

namespace Integrations.RabbitMQ;

public interface IRabbitMqPublisher
{
    Task<(bool success, string? errorMessage)> PublishAsync(byte[] body, Dictionary<string, object?>? headers, string? correlationId,
        string routingKey, bool requireConfirmation, CancellationToken cancellationToken);

    Task<(bool success, string? errorMessage)> PublishAsync(IChannel channel, byte[] body, Dictionary<string, object?>? headers, string? correlationId,
        string routingKey, bool requireConfirmation, CancellationToken cancellationToken);
}

public class RabbitMqPublisher(RabbitMqPublisherChannelPool publisherChannelPool) : IRabbitMqPublisher
{
    [SuppressMessage("Design", "CA1031:Do not catch general exception types")]
    public async Task<(bool success, string? errorMessage)> PublishAsync(byte[] body, Dictionary<string, object?>? headers, string? correlationId,
        string routingKey, bool requireConfirmation, CancellationToken cancellationToken)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(body);

            await using var channel = await publisherChannelPool.RentChannelAsync(cancellationToken);

            return await PublishInternalAsync(channel.Channel, body, headers, correlationId, routingKey, requireConfirmation, cancellationToken);
        }
        catch (Exception ex)
        {
            // According to docs, BasicPublishAsync will throw if the message could not be routed
            return (false, ex.Message);
        }
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types")]
    public async Task<(bool success, string? errorMessage)> PublishAsync(IChannel channel, byte[] body, Dictionary<string, object?>? headers,
        string? correlationId, string routingKey, bool requireConfirmation, CancellationToken cancellationToken)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(body);

            return await PublishInternalAsync(channel, body, headers, correlationId, routingKey, requireConfirmation, cancellationToken);
        }
        catch (Exception ex)
        {
            // According to docs, BasicPublishAsync will throw if the message could not be routed
            return (false, ex.Message);
        }
    }

    private static async Task<(bool success, string? errorMessage)> PublishInternalAsync(IChannel channel, byte[] body, Dictionary<string, object?>? headers,
        string? correlationId, string routingKey, bool requireConfirmation, CancellationToken cancellationToken)
    {
        var props = new BasicProperties
        {
            Persistent = false,
            ContentType = "application/json",
            CorrelationId = correlationId,
            Headers = headers
        };

        await channel.BasicPublishAsync(
            exchange: Constants.Exchange,
            routingKey: routingKey,
            mandatory: requireConfirmation,
            basicProperties: props,
            body: body,
            cancellationToken: cancellationToken);

        return (true, null);
    }
}