using Hosting.Runtime.Extensions;
using Integrations.RabbitMQ;
using Integrations.RabbitMQ.HostingExtensions;
using Integrations.Redis.HostingExtensions;
using Microsoft.Extensions.Hosting;
using NotificationOrchestrator.Services;
using Persistence.Postgres.HostingExtensions;
using RabbitMQTopology = Integrations.RabbitMQ.Topology;

var builder = Host.CreateApplicationBuilder();

builder.AddConsoleLogging();

builder
    .AddRabbitMq<RabbitMQTopology.OrchestratorTopologyHostedService>()
    .AddRabbitMqListener<OrchestrationHandler>(options =>
    {
        options.ListeningQueueName = Constants.Queues.Orchestrator;

        // A lot of work with a lot of parallelization under the hood.
        // So we should process one message at a time. Prefer horizontal scaling to process more instead.
        options.PrefetchCount = 1;
    })
    .AddRabbitMqPublisher(publisherOptions => { publisherOptions.InitialChannelCount = 10; });

builder.AddPostgresDatabase();

builder.AddRedis().WithPersistenceStoreDecorators();

using var app = builder.Build();

await app.RunAsync();