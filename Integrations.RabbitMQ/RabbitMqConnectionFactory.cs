using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace Integrations.RabbitMQ;

public sealed class RabbitMqConnectionFactory(IConfiguration configuration, ILogger<RabbitMqConnectionFactory> logger)
    : IAsyncDisposable
{
    private IConnection? connection;

    public async Task<IConnection> GetConnectionAsync(CancellationToken cancellationToken)
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

        var factory = new ConnectionFactory
        {
            HostName = configuration["RabbitMQ:Host"] ?? "localhost",
            Port = int.Parse(configuration["RabbitMQ:Port"] ?? "5672"),
            UserName = configuration["RabbitMQ:Username"] ?? "guest",
            Password = configuration["RabbitMQ:Password"] ?? "guest",
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(5)
        };

        // TODO: Rewrite in Polly
        const int maxRetries = 5;
        const int delayMilliseconds = 5000;
        var retryCount = 0;
        while (true)
        {
            try
            {
                connection = await factory.CreateConnectionAsync(cancellationToken);

                connection.ConnectionShutdownAsync += (_, args) =>
                {
                    logger.LogWarning("RabbitMQ Connection lost: {Reason}", args.ReplyText);
                    return Task.CompletedTask;
                };

                logger.LogInformation("RabbitMq connection created successfully.");

                return connection;
            }
            catch (BrokerUnreachableException) when (retryCount < maxRetries)
            {
                retryCount++;
                logger.LogWarning("RabbitMQ not ready. Retrying in {Delay}s... (Attempt {Count}/{MaxRetries})"
                    , delayMilliseconds / 1000, retryCount, maxRetries);

                await Task.Delay(delayMilliseconds, cancellationToken);
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (connection is not null)
        {
            await connection.CloseAsync();
            await connection.DisposeAsync();
        }
    }
}