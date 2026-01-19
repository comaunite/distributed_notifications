using System.Net;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Hosting;

namespace Hosting.Extensions;

public static class RateLimiterHostingExtension
{
    public static IHostApplicationBuilder AddRateLimiter(this IHostApplicationBuilder builder)
    {
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

            options.OnRejected = async delegate(OnRejectedContext context, CancellationToken cancellationToken)
            {
                // We'd want to log some metrics here in a real-world scenario

                context.HttpContext.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
                await context.HttpContext.Response.WriteAsync("Rate limit exceeded. Try again later.", cancellationToken);
            };
        });

        return builder;
    }


    // Ideally, we want to be smart here and block only specific clients that are spamming requests
    // instead of blocking all clients when the global limit is reached.
    // However, for simplicity, we are using a path-based global rate limit in this example.
    private static string GetPartitionKey(HttpContext context) => context.Request.Path;
}