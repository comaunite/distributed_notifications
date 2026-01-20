using Hosting.Runtime.Extensions;
using Integrations.RabbitMQ.HostingExtensions;
using Microsoft.Extensions.Hosting;
using NotificationWorker.Push.Handlers;
using RabbitMQTopology = Integrations.RabbitMQ.Topology;

var builder = Host.CreateApplicationBuilder();

builder.AddConsoleLogging();

builder.AddRabbitMq<RabbitMQTopology.PushWorkerTopologyHostedService, PushMessageHandler>();

using var app = builder.Build();

await app.RunAsync();