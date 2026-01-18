using Integrations.RabbitMQ.Factories;
using Integrations.RabbitMQ.Topology.Helpers;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;

namespace Integrations.RabbitMQ.Topology;

public sealed class OrchestratorTopologyHostedService(IRabbitMqConnectionFactory connectionFactory) : IHostedService
{
    private static string Exchange => Constants.Exchange;
    private static string Queue => Constants.Queues.Orchestrator;
    private static string RoutingKey => Constants.RoutingKeys.NotificationCreated;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
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