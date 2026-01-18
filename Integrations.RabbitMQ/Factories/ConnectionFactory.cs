using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace Integrations.RabbitMQ.Factories;

public interface IRabbitMqConnectionFactory
{
    Task<IConnection> CreateConnectionAsync(CancellationToken ct);
}

public class RabbitMqConnectionFactory(IConfiguration configuration, ILogger<RabbitMqConnectionFactory> logger) : IRabbitMqConnectionFactory
{
    public async Task<IConnection> CreateConnectionAsync(CancellationToken ct)
    {
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
        var retryCount = 0;
        while (true)
        {
            try
            {
                return await factory.CreateConnectionAsync(ct);
            }
            catch (BrokerUnreachableException) when (retryCount < 5)
            {
                retryCount++;
                logger.LogWarning("RabbitMQ not ready. Retrying in 5s... (Attempt {Count}/5)", retryCount);
                await Task.Delay(5000, ct);
            }
        }
    }
}