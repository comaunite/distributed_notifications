using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Persistence.Postgres.Stores;
using Persistence.Stores;

namespace Persistence.Postgres.HostingExtensions;

public static class PostgresHostingExtensions
{
    public static IHostApplicationBuilder AddPostgresDatabase(this IHostApplicationBuilder builder)
    {
        builder.Services.Configure<NotificationDbContextFactoryOptions>(options =>
        {
            options.ReadWriteConnectionString = builder.Configuration.GetConnectionString("Database")
                                                ?? Environment.GetEnvironmentVariable("ConnectionStrings__Database");
            options.ReadOnlyConnectionString = builder.Configuration.GetConnectionString("ReadOnlyDatabase")
                                               ?? Environment.GetEnvironmentVariable("ConnectionStrings__ReadOnlyDatabase");
        });

        builder.Services.AddSingleton<IDbContextFactory, NotificationDbContextFactory>();

        // Stores
        builder.Services.AddScoped<INotificationStore, PostgresNotificationStore>();

        return builder;
    }
}

public record NotificationDbContextFactoryOptions
{
    public string? ReadWriteConnectionString { get; set; }
    public string? ReadOnlyConnectionString { get; set; }
}