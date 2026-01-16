using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Hosting.Middleware;

[SuppressMessage("Design", "CA1062: Validate arguments of public methods")]
public partial class GlobalExceptionHandlingMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlingMiddleware> logger)
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "An unhandled exception occurred while processing the request.")]
    private static partial void LogUnhandledException(ILogger logger, Exception ex);

    [SuppressMessage("Design", "CA1031:Do not catch general exception types")]
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            LogUnhandledException(logger, ex);

            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";

            var response = new
            {
                Message = "An unexpected error occurred. Please try again later."
            };

            await context.Response.WriteAsJsonAsync(response);
        }
    }
}