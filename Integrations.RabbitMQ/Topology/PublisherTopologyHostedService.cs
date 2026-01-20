using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;

namespace Integrations.RabbitMQ.Topology;

public sealed class PublisherTopologyHostedService(RabbitMqPublisherChannelPool publisherChannelPool) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var channel = await publisherChannelPool.RentChannelAsync(cancellationToken);

        await channel.ExchangeDeclareAsync(
            exchange: Constants.Exchange,
            type: ExchangeType.Topic,
            durable: true,
            cancellationToken: cancellationToken);

        await publisherChannelPool.ReturnChannelAsync(channel);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}