using Integrations.RabbitMQ.Factories;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;

namespace Integrations.RabbitMQ.Topology;

public sealed class PublisherTopologyHostedService(IRabbitMqChannelPool channelPool) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var channel = await channelPool.RentChannelAsync(cancellationToken);

        await channel.ExchangeDeclareAsync(
            exchange: Constants.Exchange,
            type: ExchangeType.Topic,
            durable: true,
            cancellationToken: cancellationToken);

        channelPool.ReturnChannel(channel);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}