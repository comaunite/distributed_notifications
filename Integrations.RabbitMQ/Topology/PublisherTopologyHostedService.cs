using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;

namespace Integrations.RabbitMQ.Topology;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class PublisherTopologyHostedService(RabbitMqPublisherChannelPool publisherChannelPool) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var channel = await publisherChannelPool.RentChannelAsync(cancellationToken);

        await channel.Channel.ExchangeDeclareAsync(
            exchange: Constants.Exchange,
            type: ExchangeType.Topic,
            durable: true,
            cancellationToken: cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}