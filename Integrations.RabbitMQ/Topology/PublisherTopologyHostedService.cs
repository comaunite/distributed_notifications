using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;

namespace Integrations.RabbitMQ.Topology;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class PublisherTopologyHostedService(RabbitMqConnectionFactory connectionFactory) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // Don't need to dispose of the connection here, as it's managed by the connection factory
        var connection = await connectionFactory.GetConnectionAsync(cancellationToken);

        await using var channel = await connection.CreateChannelAsync(null, cancellationToken);

        await channel.ExchangeDeclareAsync(
            exchange: Constants.Exchange,
            type: ExchangeType.Topic,
            durable: true,
            cancellationToken: cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}