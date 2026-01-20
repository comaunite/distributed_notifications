using Hosting.Runtime.Extensions;
using Integrations.RabbitMQ;
using Integrations.RabbitMQ.HostingExtensions;
using Microsoft.Extensions.Hosting;
using NotificationOrchestrator.Services;
using Persistence.Postgres.HostingExtensions;
using RabbitMQTopology = Integrations.RabbitMQ.Topology;

var builder = Host.CreateApplicationBuilder();

builder.AddConsoleLogging();

builder.AddRabbitMqListenerAndPublisher<RabbitMQTopology.OrchestratorTopologyHostedService, OrchestrationHandler>(
    options =>
    {
        options.ListeningQueueName = Constants.Queues.Orchestrator;

        // A lot of work with a lot of parallelization under the hood.
        // So we should process one message at a time. Prefer horizontal scaling to process more instead.
        options.PrefetchCount = 1;
    },
    publisherOptions =>
    {
        publisherOptions.InitialChannelCount = 10;
    });

builder.AddPostgresDatabase();

using var app = builder.Build();

await app.RunAsync();