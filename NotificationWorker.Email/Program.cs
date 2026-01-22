using Hosting.Runtime.Extensions;
using Integrations.RabbitMQ;
using Integrations.RabbitMQ.HostingExtensions;
using Integrations.Redis.HostingExtensions;
using Microsoft.Extensions.Hosting;
using NotificationWorker.Email.Handlers;
using RabbitMQTopology = Integrations.RabbitMQ.Topology;

var builder = Host.CreateApplicationBuilder();

builder.AddConsoleLogging();

builder
    .AddRabbitMq<RabbitMQTopology.EmailWorkerTopologyHostedService>()
    .AddRabbitMqListener<EmailMessageHandler>(options =>
    {
        options.ListeningQueueName = Constants.Queues.EmailWorker;
        options.PrefetchCount = 20;
    })
    .AddRabbitMqPublisher(publisherOptions => { publisherOptions.InitialChannelCount = 10; });

builder.AddRedis();

using var app = builder.Build();

await app.RunAsync();