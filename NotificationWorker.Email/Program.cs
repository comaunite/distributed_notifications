using Hosting.Runtime.Extensions;
using Integrations.RabbitMQ;
using Integrations.RabbitMQ.Factories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NotificationWorker.Email.Handlers;
using RabbitMQTopology = Integrations.RabbitMQ.Topology;

var builder = Host.CreateApplicationBuilder();

builder.AddConsoleLogging();

builder.Services.AddSingleton<RabbitMqConnectionFactory>();
builder.Services.AddSingleton<IRabbitMqChannelPool, RabbitMqChannelPool>();
builder.Services.AddHostedService<RabbitMQTopology.EmailWorkerTopologyHostedService>();

builder.Services.AddSingleton<EmailMessageHandler>();
builder.Services.AddHostedService<QueueConsumerService<EmailMessageHandler>>();

using var app = builder.Build();

await app.RunAsync();