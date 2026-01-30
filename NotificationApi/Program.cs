using Hosting.Extensions;
using Hosting.Middleware;
using Integrations.RabbitMQ.Extensions;
using Integrations.Redis.HostingExtensions;
using NotificationApi.Models;
using NotificationApi.Services;
using Persistence.Postgres.HostingExtensions;
using RabbitMQTopology = Integrations.RabbitMQ.Topology;


var builder = WebApplication.CreateBuilder(args);

builder.AddApiLogging();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddOpenApi();

builder
    .AddRabbitMq<RabbitMQTopology.PublisherTopologyHostedService>()
    .AddRabbitMqPublisher(options => { options.InitialChannelCount = 3; });

builder.Services.AddScoped<INotificationService, NotificationService>();

builder.AddRateLimiter();
builder.AddPostgresDatabase();
builder.AddRedis().WithPersistenceStoreDecorators();

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