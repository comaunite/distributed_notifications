using Integrations.RabbitMQ;
using Integrations.RabbitMQ.Factories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NotificationOrchestrator.Services;
using RabbitMQTopology = Integrations.RabbitMQ.Topology;

var builder = Host.CreateApplicationBuilder();

builder.Logging.AddConsole(options =>
{
    options.FormatterName = "simple";
});
builder.Logging.AddDebug();

builder.Services.AddSingleton<IRabbitMqConnectionFactory, RabbitMqConnectionFactory>();
builder.Services.AddSingleton<OrchestrationHandler>();

builder.Services.AddHostedService<RabbitMQTopology.OrchestratorTopologyHostedService>();
builder.Services.AddHostedService<QueueConsumerService<OrchestrationHandler>>();

using var app = builder.Build();

await app.RunAsync();