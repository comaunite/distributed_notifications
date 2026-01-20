using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
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
        var connectionString = builder.Configuration.GetConnectionString("Database")
                               ?? Environment.GetEnvironmentVariable("ConnectionStrings__Database");

        builder.Services.AddDbContextPool<NotificationDbContext>(options =>
        {
            options.UseNpgsql(connectionString, opts =>
            {
                opts.UseAdminDatabase("postgres");
                opts.EnableRetryOnFailure();
                opts.MigrationsHistoryTable(HistoryRepository.DefaultTableName, "public");
            });

            // This is to be removed for the production environment
            options.LogTo(s => Debug.WriteLine(s));
            options
                .EnableDetailedErrors()
                .EnableSensitiveDataLogging();
        });

        var readOnlyConnectionString = builder.Configuration.GetConnectionString("ReadOnlyDatabase")
                                       ?? Environment.GetEnvironmentVariable("ConnectionStrings__ReadOnlyDatabase");

        builder.Services.AddDbContextPool<ReadOnlyNotificationDbContext>(options =>
        {
            options.UseNpgsql(readOnlyConnectionString, opts =>
            {
                opts.UseAdminDatabase("postgres");
                opts.EnableRetryOnFailure();
                opts.MigrationsHistoryTable(HistoryRepository.DefaultTableName, "public");
            });

            // This is to be removed for the production environment
            options.LogTo(s => Debug.WriteLine(s));
            options
                .EnableDetailedErrors()
                .EnableSensitiveDataLogging();
        });

        // Stores
        builder.Services.AddScoped<INotificationStore, PostgresNotificationStore>();

        return builder;
    }
}