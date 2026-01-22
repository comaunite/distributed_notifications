using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Persistence.Postgres.Stores;
using Persistence.Stores;

namespace Persistence.Postgres.HostingExtensions;

public static class PostgresHostingExtensions
{
    public static IHostApplicationBuilder AddPostgresDatabase(this IHostApplicationBuilder builder)
    {
        var connectionString = builder.Configuration.GetConnectionString("Database")
                               ?? Environment.GetEnvironmentVariable("ConnectionStrings__Database");

        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
        dataSourceBuilder.EnableDynamicJson();
        var dataSource = dataSourceBuilder.Build();

        builder.Services.AddDbContextPool<NotificationDbContext>(options =>
        {
            options.UseNpgsql(dataSource, opts =>
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

        var readOnlyDataSourceBuilder = new NpgsqlDataSourceBuilder(readOnlyConnectionString);
        readOnlyDataSourceBuilder.EnableDynamicJson();
        var readOnlyDataSource = readOnlyDataSourceBuilder.Build();

        builder.Services.AddDbContextPool<ReadOnlyNotificationDbContext>(options =>
        {
            options.UseNpgsql(readOnlyDataSource, opts =>
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