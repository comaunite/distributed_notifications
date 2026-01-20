using Microsoft.Extensions.Hosting;

namespace Integrations.RabbitMQ;

public class RabbitMqWarmupService(RabbitMqPublisherChannelPool pool)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await pool.WarmupAsync(stoppingToken);
    }
}