using System.Diagnostics.CodeAnalysis;
using Integrations.RabbitMQ.Topology.Helpers;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;

// ReSharper disable once CheckNamespace
namespace Integrations.RabbitMQ.Topology;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class PushWorkerTopologyHostedService(RabbitMqPublisherChannelPool publisherChannelPool) : IHostedService
{
    private static string Exchange => Constants.Exchange;
    private static string Queue => Constants.Queues.PushWorker;
    private static string RoutingKey => Constants.RoutingKeys.SendPush;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var channel = await publisherChannelPool.RentChannelAsync(cancellationToken);

        var dlqArgs = await DlqHelper.InitDeadLetterQueueAsync(channel.Channel, Exchange, Queue, cancellationToken);

        await channel.Channel.ExchangeDeclareAsync(
            exchange: Exchange,
            type: ExchangeType.Topic,
            durable: true,
            cancellationToken: cancellationToken);

        await channel.Channel.QueueDeclareAsync(
            queue: Queue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: dlqArgs,
            cancellationToken: cancellationToken);

        await channel.Channel.QueueBindAsync(
            queue: Queue,
            exchange: Exchange,
            routingKey: RoutingKey,
            cancellationToken: cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}