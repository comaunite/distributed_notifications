using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Integrations.RabbitMQ;

[SuppressMessage("Performance", "CA1815:Override equals and operator equals on value types")]
public readonly struct RentedChannel(IChannel channel, RabbitMqPublisherChannelPool pool) : IAsyncDisposable
{
    public IChannel Channel => channel;

    public ValueTask DisposeAsync()
    {
        return pool.ReturnChannelAsync(channel);
    }
}

public class RabbitMqPublisherChannelPoolOptions
{
    public int InitialChannelCount { get; set; } = 5;
}

public sealed class RabbitMqPublisherChannelPool(RabbitMqConnectionFactory connectionFactory, IOptions<RabbitMqPublisherChannelPoolOptions> options,
    ILogger<RabbitMqPublisherChannelPool> logger)
    : IAsyncDisposable
{
    private readonly ConcurrentStack<IChannel> channels = new();
    private IConnection? connection;
    private readonly SemaphoreSlim connectionLock = new(1, 1);

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Disposed in DisposeAsync")]
    public async ValueTask<RentedChannel> RentChannelAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Renting RabbitMq Channel from pool...");

        if (channels.TryPop(out var channel) && channel.IsOpen)
        {
            return new RentedChannel(channel, this);
        }

        await EnsureConnectionAsync(cancellationToken);

        logger.LogInformation("RabbitMq Channel Pool is empty, creating new channel...");

        channel = await SpawnChannelAsync(cancellationToken);

        return new RentedChannel(channel, this);
    }

    public async Task WarmupAsync(CancellationToken cancellationToken)
    {
        if (options.Value.InitialChannelCount <= 0)
        {
            logger.LogInformation("RabbitMq Channel Pool warmup skipped, InitialChannelCount is set to {InitialChannelCount}.",
                options.Value.InitialChannelCount);
            return;
        }

        if (!channels.IsEmpty)
        {
            logger.LogInformation("RabbitMq Channel Pool warmup skipped, pool is not empty.");
            return;
        }

        await EnsureConnectionAsync(cancellationToken);

        logger.LogInformation("Warming up RabbitMq Channel Pool with {InitialChannelCount} channels...", options.Value.InitialChannelCount);

        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount,
            CancellationToken = cancellationToken
        };

        await Parallel.ForAsync(0, options.Value.InitialChannelCount, parallelOptions, async (_, ct) =>
        {
            var channel = await SpawnChannelAsync(ct);

            channels.Push(channel);
        });

        logger.LogInformation("RabbitMq Channel Pool warmup complete.");
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

    private async Task EnsureConnectionAsync(CancellationToken cancellationToken)
    {
        await connectionLock.WaitAsync(cancellationToken);

        try
        {
            if (connection is { IsOpen: true })
            {
                return;
            }

            connection = await connectionFactory.GetConnectionAsync(cancellationToken);
        }
        finally
        {
            connectionLock.Release();
        }
    }

    private async Task<IChannel> SpawnChannelAsync(CancellationToken cancellationToken)
    {
        return await connection!.CreateChannelAsync(new CreateChannelOptions(
            publisherConfirmationsEnabled: true,
            publisherConfirmationTrackingEnabled: true
        ), cancellationToken);
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