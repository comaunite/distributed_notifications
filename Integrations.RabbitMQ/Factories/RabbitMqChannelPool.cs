using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Integrations.RabbitMQ.Factories;

public interface IRabbitMqChannelPool
{
    Task<IChannel> RentChannelAsync(CancellationToken cancellationToken);
    void ReturnChannel(IChannel channel);
}

public sealed class RabbitMqChannelPool(RabbitMqConnectionFactory connectionFactory, ILogger<RabbitMqChannelPool> logger)
    : IRabbitMqChannelPool, IAsyncDisposable
{
    private readonly ConcurrentStack<IChannel> channels = new();
    private IConnection? connection;
    private readonly SemaphoreSlim connectionLock = new(1, 1);

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Disposed in DisposeAsync")]
    public async Task<IChannel> RentChannelAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Renting RabbitMq Channel from pool...");

        if (channels.TryPop(out var channel) && channel.IsOpen)
        {
            return channel;
        }

        await EnsureConnectionAsync(cancellationToken);

        logger.LogInformation("RabbitMq Channel Pool is empty, creating new channel...");

        return await connection!.CreateChannelAsync(new CreateChannelOptions(
            publisherConfirmationsEnabled: true,
            publisherConfirmationTrackingEnabled: true
        ), cancellationToken);
    }

    private async Task EnsureConnectionAsync(CancellationToken cancellationToken)
    {
        await connectionLock.WaitAsync(cancellationToken);

        try
        {
            connection ??= await connectionFactory.CreateConnectionAsync(cancellationToken);
        }
        finally
        {
            connectionLock.Release();
        }
    }

    public void ReturnChannel(IChannel channel)
    {
        if (channel.IsOpen)
        {
            channels.Push(channel);
        }
        else
        {
            channel.Dispose();
        }
    }

    public async ValueTask DisposeAsync()
    {
        while (channels.TryPop(out var channel))
        {
            await channel.CloseAsync();
            await channel.DisposeAsync();
        }

        if (connection != null)
        {
            await connection.CloseAsync();
            await connection.DisposeAsync();
        }

        connectionLock.Dispose();
    }
}