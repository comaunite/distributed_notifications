using System.Diagnostics.CodeAnalysis;
using Integrations.RabbitMQ.Topology.Helpers;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;

// ReSharper disable once CheckNamespace
namespace Integrations.RabbitMQ.Topology;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class PushWorkerTopologyHostedService(RabbitMqConnectionFactory connectionFactory) : IHostedService
{
    private static string Exchange => Constants.Exchange;
    private static string Queue => Constants.Queues.PushWorker;
    private static string RoutingKey => Constants.RoutingKeys.SendPush;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // Don't need to dispose of the connection here, as it's managed by the connection factory
        var connection = await connectionFactory.GetConnectionAsync(cancellationToken);

        await using var channel = await connection.CreateChannelAsync(null, cancellationToken);

        var dlqArgs = await DlqHelper.InitDeadLetterQueueAsync(channel, Exchange, Queue, cancellationToken);

        await channel.ExchangeDeclareAsync(
            exchange: Exchange,
            type: ExchangeType.Topic,
            durable: true,
            cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(
            queue: Queue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: dlqArgs,
            cancellationToken: cancellationToken);

        await channel.QueueBindAsync(
            queue: Queue,
            exchange: Exchange,
            routingKey: RoutingKey,
            cancellationToken: cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}