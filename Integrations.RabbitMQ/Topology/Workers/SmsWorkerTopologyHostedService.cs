using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;

// ReSharper disable once CheckNamespace
namespace Integrations.RabbitMQ.Topology;

public sealed class SmsWorkerTopologyHostedService(IRabbitMqConnectionFactory connectionFactory) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        await using var channel = await connection.CreateChannelAsync(null, cancellationToken);

        await channel.ExchangeDeclareAsync(
            exchange: Constants.Exchange,
            type: ExchangeType.Topic,
            durable: true,
            cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(
            queue: Constants.Queues.SmsWorker,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: cancellationToken);

        await channel.QueueBindAsync(
            queue: Constants.Queues.SmsWorker,
            exchange: Constants.Exchange,
            routingKey: Constants.RoutingKeys.SendSms,
            cancellationToken: cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}