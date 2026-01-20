using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Integrations.RabbitMQ;

public sealed class RabbitMqChannelPool(RabbitMqConnectionFactory connectionFactory, ILogger<RabbitMqChannelPool> logger)
    : IAsyncDisposable
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
            if (connection is { IsOpen: true })
            {
                return;
            }

            connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        }
        finally
        {
            connectionLock.Release();
        }
    }

    public async ValueTask ReturnChannelAsync(IChannel channel)
    {
        if (channel.IsOpen)
        {
            logger.LogInformation("Returning RabbitMq Channel to pool...");

            channels.Push(channel);
        }
        else
        {
            logger.LogInformation("RabbitMq Channel is closed, disposing...");

            await channel.DisposeAsync();
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