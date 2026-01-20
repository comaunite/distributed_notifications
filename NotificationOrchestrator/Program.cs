using Hosting.Runtime.Extensions;
using Integrations.RabbitMQ.HostingExtensions;
using Microsoft.Extensions.Hosting;
using NotificationOrchestrator.Services;
using Persistence.Postgres.HostingExtensions;
using RabbitMQTopology = Integrations.RabbitMQ.Topology;

var builder = Host.CreateApplicationBuilder();

builder.AddConsoleLogging();

builder.AddRabbitMq<RabbitMQTopology.OrchestratorTopologyHostedService, OrchestrationHandler>();

builder.AddPostgresDatabase(withReadonlyReplica: false);

using var app = builder.Build();

await app.RunAsync();