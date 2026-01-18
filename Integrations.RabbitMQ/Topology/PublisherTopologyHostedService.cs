using Integrations.RabbitMQ.Factories;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;

namespace Integrations.RabbitMQ.Topology;

public sealed class PublisherTopologyHostedService(IRabbitMqConnectionFactory connectionFactory) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        await using var channel = await connection.CreateChannelAsync(
            new CreateChannelOptions(
                publisherConfirmationsEnabled: true,
                publisherConfirmationTrackingEnabled: true
            ), cancellationToken);

        await channel.ExchangeDeclareAsync(
            exchange: Constants.Exchange,
            type: ExchangeType.Topic,
            durable: true,
            cancellationToken: cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}