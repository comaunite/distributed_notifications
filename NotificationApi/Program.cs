using System.Net;
using System.Threading.RateLimiting;
using Hosting.Middleware;
using Integrations.RabbitMQ.Factories;
using Microsoft.AspNetCore.RateLimiting;
using NotificationApi.Models;
using NotificationApi.Services;
using RabbitMQTopology = Integrations.RabbitMQ.Topology;


var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.SetMinimumLevel(LogLevel.Information);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddOpenApi();

builder.Services.AddSingleton<IRabbitMqConnectionFactory, RabbitMqConnectionFactory>();
builder.Services.AddSingleton<INotificationService, NotificationService>();

builder.Services.AddHostedService<RabbitMQTopology.PublisherTopologyHostedService>();

builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: GetPartitionKey(httpContext),
            factory: _ => new SlidingWindowRateLimiterOptions
            {
                Window = TimeSpan.FromSeconds(30),
                PermitLimit = 10,
                SegmentsPerWindow = 3
            }
        )
    );

    options.OnRejected = async delegate(OnRejectedContext context, CancellationToken token)
    {
        // We'd want to log some metrics here in a real-world scenario

        context.HttpContext.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
        await context.HttpContext.Response.WriteAsync("Rate limit exceeded. Try again later.", cancellationToken: token);
    };
});

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
        async (NotificationRequest request, INotificationService notificationService, CancellationToken ct) =>
            await notificationService.PostNotificationAsync(request, ct))
    .WithName("SendNotification");

app.Run();
return;

// Ideally, we want to be smart here and block only specific clients that are spamming requests
// instead of blocking all clients when the global limit is reached.
// However, for simplicity, we are using a path-based global rate limit in this example.
static string GetPartitionKey(HttpContext context) => context.Request.Path;