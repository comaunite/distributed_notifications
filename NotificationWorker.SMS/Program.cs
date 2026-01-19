using Hosting.Extensions;
using Integrations.RabbitMQ;
using Integrations.RabbitMQ.Factories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NotificationWorker.SMS.Handlers;
using RabbitMQTopology = Integrations.RabbitMQ.Topology;

var builder = Host.CreateApplicationBuilder();

builder.AddConsoleLogging();

builder.Services.AddSingleton<IRabbitMqConnectionFactory, RabbitMqConnectionFactory>();
builder.Services.AddSingleton<SmsMessageHandler>();

builder.Services.AddHostedService<RabbitMQTopology.SmsWorkerTopologyHostedService>();
builder.Services.AddHostedService<QueueConsumerService<SmsMessageHandler>>();

using var app = builder.Build();

await app.RunAsync();