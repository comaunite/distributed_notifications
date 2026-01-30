using Microsoft.Extensions.Hosting;

namespace Integrations.RabbitMQ;

public class RabbitMqChannelPoolWarmupService(RabbitMqPublisherChannelPool pool)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await pool.WarmupAsync(stoppingToken);
    }
}