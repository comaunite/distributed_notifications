using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace NotificationOrchestrator.Services;

#pragma warning disable CA1812
// Hosted services are not directly instantiated
internal sealed class OrchestratorService(ILogger<OrchestratorService> logger) : BackgroundService
#pragma warning restore CA1812
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Notification Orchestrator Service is running.");

        return Task.CompletedTask;
    }
}