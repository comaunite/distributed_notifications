using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Hosting.Runtime.Extensions;

[SuppressMessage("ReSharper", "ConvertToExtensionBlock")]
public static class LoggingExtensions
{
    public static IHostApplicationBuilder AddConsoleLogging(this IHostApplicationBuilder builder)
    {
        builder.Logging.AddConsole(options =>
        {
            options.FormatterName = "simple";
        });
        builder.Logging.AddDebug();

        return builder;
    }
}