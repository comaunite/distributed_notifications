using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Hosting.Extensions;

[SuppressMessage("ReSharper", "ConvertToExtensionBlock")]
public static class LoggingExtensions
{
    public static IHostApplicationBuilder AddApiLogging(this IHostApplicationBuilder builder)
    {
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();
        builder.Logging.AddDebug();
        builder.Logging.SetMinimumLevel(LogLevel.Information);

        return builder;
    }
}