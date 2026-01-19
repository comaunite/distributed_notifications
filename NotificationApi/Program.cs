using Database.HostingExtensions;
using Hosting.Extensions;
using Hosting.Middleware;
using Integrations.RabbitMQ.Factories;
using NotificationApi.Models;
using NotificationApi.Services;
using RabbitMQTopology = Integrations.RabbitMQ.Topology;


var builder = WebApplication.CreateBuilder(args);

builder.AddApiLogging();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddOpenApi();

builder.Services.AddSingleton<IRabbitMqConnectionFactory, RabbitMqConnectionFactory>();
builder.Services.AddSingleton<INotificationService, NotificationService>();

builder.Services.AddHostedService<RabbitMQTopology.PublisherTopologyHostedService>();

builder.AddRateLimiter();
builder.AddPostgresDatabase();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseRateLimiter();
app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

app.UseSwagger();
app.UseSwaggerUI();

app.MapPost("/send-notification",
        async (NotificationRequest request, INotificationService notificationService, CancellationToken cancellationToken) =>
            await notificationService.PostNotificationAsync(request, cancellationToken))
    .WithName("SendNotification");

app.Run();