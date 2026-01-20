using Integrations.RabbitMQ.Topology.Helpers;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;

// ReSharper disable once CheckNamespace
namespace Integrations.RabbitMQ.Topology;

public sealed class EmailWorkerTopologyHostedService(RabbitMqChannelPool channelPool) : IHostedService
{
    private static string Exchange => Constants.Exchange;
    private static string Queue => Constants.Queues.EmailWorker;
    private static string RoutingKey => Constants.RoutingKeys.SendEmail;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var channel = await channelPool.RentChannelAsync(cancellationToken);

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

        await channelPool.ReturnChannelAsync(channel);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}