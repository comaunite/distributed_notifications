using Hosting.Runtime.Extensions;
using Integrations.RabbitMQ;
using Integrations.RabbitMQ.HostingExtensions;
using Microsoft.Extensions.Hosting;
using NotificationWorker.SMS.Handlers;
using RabbitMQTopology = Integrations.RabbitMQ.Topology;

var builder = Host.CreateApplicationBuilder();

builder.AddConsoleLogging();

builder.AddRabbitMq<RabbitMQTopology.SmsWorkerTopologyHostedService, SmsMessageHandler>(options =>
{
    options.ListeningQueueName = Constants.Queues.SmsWorker;
    options.PrefetchCount = 20;
});

using var app = builder.Build();

await app.RunAsync();