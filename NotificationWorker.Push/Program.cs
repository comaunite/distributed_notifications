using Hosting.Runtime.Extensions;
using Integrations.RabbitMQ;
using Integrations.RabbitMQ.HostingExtensions;
using Microsoft.Extensions.Hosting;
using NotificationWorker.Push.Handlers;
using RabbitMQTopology = Integrations.RabbitMQ.Topology;

var builder = Host.CreateApplicationBuilder();

builder.AddConsoleLogging();

builder.AddRabbitMqListenerAndPublisher<RabbitMQTopology.PushWorkerTopologyHostedService, PushMessageHandler>(
    options =>
    {
        options.ListeningQueueName = Constants.Queues.PushWorker;
        options.PrefetchCount = 20;
    },
    publisherOptions =>
    {
        publisherOptions.InitialChannelCount = 10;
    });

using var app = builder.Build();

await app.RunAsync();