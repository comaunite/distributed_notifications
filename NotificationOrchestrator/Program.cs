using Hosting.Extensions;
using Integrations.RabbitMQ;
using Integrations.RabbitMQ.Factories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NotificationOrchestrator.Services;
using Persistence.Postgres.HostingExtensions;
using RabbitMQTopology = Integrations.RabbitMQ.Topology;

var builder = Host.CreateApplicationBuilder();

builder.AddConsoleLogging();

builder.Services.AddSingleton<IRabbitMqConnectionFactory, RabbitMqConnectionFactory>();
builder.Services.AddSingleton<OrchestrationHandler>();

builder.Services.AddHostedService<RabbitMQTopology.OrchestratorTopologyHostedService>();
builder.Services.AddHostedService<QueueConsumerService<OrchestrationHandler>>();

builder.AddPostgresDatabase();

using var app = builder.Build();

await app.RunAsync();