using RabbitMQ.Client;

namespace Integrations.RabbitMQ;

public interface IRabbitMqConnectionFactory
{
    Task<IConnection> CreateConnectionAsync(CancellationToken ct);
}

public class RabbitMqConnectionFactory : IRabbitMqConnectionFactory
{
    public async Task<IConnection> CreateConnectionAsync(CancellationToken ct)
    {
        var factory = new ConnectionFactory
        {
            HostName = "localhost",
            Port = 5672,
            UserName = "guest",
            Password = "guest",
        };

        return await factory.CreateConnectionAsync(ct);
    }
}