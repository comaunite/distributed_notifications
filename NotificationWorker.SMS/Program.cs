using Hosting.Runtime.Extensions;
using Integrations.RabbitMQ;
using Integrations.RabbitMQ.Factories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NotificationWorker.SMS.Handlers;
using RabbitMQTopology = Integrations.RabbitMQ.Topology;

var builder = Host.CreateApplicationBuilder();

builder.AddConsoleLogging();

builder.Services.AddSingleton<RabbitMqConnectionFactory>();
builder.Services.AddSingleton<IRabbitMqChannelPool, RabbitMqChannelPool>();
builder.Services.AddHostedService<RabbitMQTopology.SmsWorkerTopologyHostedService>();

builder.Services.AddSingleton<SmsMessageHandler>();
builder.Services.AddHostedService<QueueConsumerService<SmsMessageHandler>>();

using var app = builder.Build();

await app.RunAsync();