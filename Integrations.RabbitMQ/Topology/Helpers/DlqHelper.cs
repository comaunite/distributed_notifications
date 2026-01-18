using RabbitMQ.Client;

namespace Integrations.RabbitMQ.Topology.Helpers;

public static class DlqHelper
{
    public static async Task<Dictionary<string, object?>> InitDeadLetterQueueAsync(IChannel channel, string exchange,
        string queue, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(channel);

        var dlxExchange = $"{exchange}.dlx";
        var dlqQueue = $"{queue}.dlq";

        await channel.ExchangeDeclareAsync(
            exchange: dlxExchange,
            type: ExchangeType.Fanout,
            durable: true,
            cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(
            queue: dlqQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: cancellationToken);

        await channel.QueueBindAsync(
            queue: dlqQueue,
            exchange: dlxExchange,
            routingKey: string.Empty,
            cancellationToken: cancellationToken);

        return new Dictionary<string, object?>
        {
            { "x-dead-letter-exchange", dlxExchange }
        };
    }
}