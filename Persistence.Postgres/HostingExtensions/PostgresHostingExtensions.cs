using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Persistence.Stores.Interfaces;
using Persistence.Stores.Postgres;

namespace Database.HostingExtensions;

public static class PostgresHostingExtensions
{
    public static IHostApplicationBuilder AddPostgresDatabase(this IHostApplicationBuilder builder)
    {
        // We can split into separate reader and writer contexts here to target replica for reads
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

        // Allow transaction control across multiple stores
        builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Stores
        builder.Services.AddScoped<INotificationStore, PostgresNotificationStore>();

        return builder;
    }
}