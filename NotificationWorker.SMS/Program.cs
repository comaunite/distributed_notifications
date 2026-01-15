using Integrations.RabbitMQ;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQTopology = Integrations.RabbitMQ.Topology;

var builder = Host.CreateApplicationBuilder();

builder.Services.AddSingleton<IRabbitMqConnectionFactory, RabbitMqConnectionFactory>();

builder.Services.AddSingleton<IConnection>(sp =>
{
    var factory = sp.GetRequiredService<IRabbitMqConnectionFactory>();
    return factory.CreateConnectionAsync(CancellationToken.None).GetAwaiter().GetResult();
});

builder.Services.AddHostedService<RabbitMQTopology.SmsWorkerTopologyHostedService>();