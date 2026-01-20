using System.Net.Sockets;
using Integrations.RabbitMQ.HostingExtensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace Integrations.RabbitMQ;

public sealed class RabbitMqConnectionFactory(IOptions<RabbitMqConnectionOptions> options, ILogger<RabbitMqConnectionFactory> logger)
    : IAsyncDisposable
{
    private IConnection? connection;

    private readonly ResiliencePipeline<IConnection> connectionPipeline = new ResiliencePipelineBuilder<IConnection>()
        .AddRetry(new RetryStrategyOptions<IConnection>
        {
            ShouldHandle = new PredicateBuilder<IConnection>()
                .Handle<BrokerUnreachableException>()
                .Handle<SocketException>(),
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true,
            MaxRetryAttempts = options.Value.MaxRetryAttempts,
            Delay = TimeSpan.FromSeconds(options.Value.RetryDelaySeconds),
            OnRetry = args =>
            {
                logger.LogWarning("RabbitMQ connection attempt {Attempt} failed. Retrying...", args.AttemptNumber + 1);
                return default;
            }
        })
        .Build();

    private ConnectionFactory? connectionFactory;

    private readonly SemaphoreSlim semaphore = new(1, 1);

    public async Task<IConnection> GetConnectionAsync(CancellationToken cancellationToken)
    {
        await semaphore.WaitAsync(cancellationToken);

        try
        {
            if (connection is not null)
            {
                if (connection.IsOpen)
                {
                    return connection;
                }

                await DisposeAsync();
            }

            logger.LogInformation("Creating RabbitMq connection...");

            connectionFactory ??= new ConnectionFactory
            {
                HostName = options.Value.HostName,
                Port = options.Value.Port,
                UserName = options.Value.UserName,
                Password = options.Value.Password,
                AutomaticRecoveryEnabled = options.Value.AutomaticRecoveryEnabled,
                NetworkRecoveryInterval = options.Value.NetworkRecoveryInterval
            };

            connection = await connectionPipeline.ExecuteAsync(async ct =>
                await connectionFactory.CreateConnectionAsync(ct), cancellationToken);

            connection.ConnectionShutdownAsync += (_, args) =>
            {
                logger.LogWarning("RabbitMQ Connection lost: {Reason}", args.ReplyText);
                return Task.CompletedTask;
            };

            return connection;
        }
        finally
        {
            semaphore.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (connection is not null)
        {
            await connection.CloseAsync();
            await connection.DisposeAsync();
        }

        semaphore.Dispose();
    }
}