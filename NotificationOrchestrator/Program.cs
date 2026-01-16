using Integrations.RabbitMQ;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NotificationOrchestrator.Services;
using RabbitMQTopology = Integrations.RabbitMQ.Topology;

var builder = Host.CreateApplicationBuilder();

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.SetMinimumLevel(LogLevel.Information);

builder.Services.AddSingleton<IRabbitMqConnectionFactory, RabbitMqConnectionFactory>();

builder.Services.AddHostedService<RabbitMQTopology.OrchestratorTopologyHostedService>();
builder.Services.AddHostedService<OrchestratorService>();

using var app = builder.Build();

await app.RunAsync();