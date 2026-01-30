using Microsoft.Extensions.Hosting;

namespace Integrations.RabbitMQ;

public class RabbitMqChannelPoolWarmupService(RabbitMqPublisherChannelPool pool)
    : IHostedLifecycleService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await pool.WarmupAsync(cancellationToken);
    }

    public Task StartedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StartingAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StoppedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StoppingAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}