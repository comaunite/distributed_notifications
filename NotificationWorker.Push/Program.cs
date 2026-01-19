using Hosting.Extensions;
using Integrations.RabbitMQ;
using Integrations.RabbitMQ.Factories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NotificationWorker.Push.Handlers;
using RabbitMQTopology = Integrations.RabbitMQ.Topology;

var builder = Host.CreateApplicationBuilder();

builder.AddConsoleLogging();

builder.Services.AddSingleton<IRabbitMqConnectionFactory, RabbitMqConnectionFactory>();
builder.Services.AddSingleton<PushMessageHandler>();

builder.Services.AddHostedService<RabbitMQTopology.PushWorkerTopologyHostedService>();
builder.Services.AddHostedService<QueueConsumerService<PushMessageHandler>>();

using var app = builder.Build();

await app.RunAsync();