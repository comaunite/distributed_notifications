using Hosting.Runtime.Extensions;
using Integrations.RabbitMQ.HostingExtensions;
using Microsoft.Extensions.Hosting;
using NotificationWorker.Email.Handlers;
using RabbitMQTopology = Integrations.RabbitMQ.Topology;

var builder = Host.CreateApplicationBuilder();

builder.AddConsoleLogging();

builder.AddRabbitMq<RabbitMQTopology.EmailWorkerTopologyHostedService, EmailMessageHandler>();

using var app = builder.Build();

await app.RunAsync();