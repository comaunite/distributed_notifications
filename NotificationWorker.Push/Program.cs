using Integrations.RabbitMQ;
using Integrations.RabbitMQ.Factories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NotificationWorker.Push.Handlers;
using RabbitMQTopology = Integrations.RabbitMQ.Topology;

var builder = Host.CreateApplicationBuilder();

builder.Logging.AddConsole(options =>
{
    options.FormatterName = "simple";
});
builder.Logging.AddDebug();

builder.Services.AddSingleton<IRabbitMqConnectionFactory, RabbitMqConnectionFactory>();
builder.Services.AddSingleton<PushMessageHandler>();

builder.Services.AddHostedService<RabbitMQTopology.PushWorkerTopologyHostedService>();
builder.Services.AddHostedService<QueueConsumerService<PushMessageHandler>>();

using var app = builder.Build();

await app.RunAsync();