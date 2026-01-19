using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Persistence.Postgres;

namespace DbMigrator;

/// <summary>
/// This class is required in order for dotnet ef commands to work, such as adding new migrations.
/// </summary>
#pragma warning disable CA1515
// public/internal: Type must be discoverable by CLI tools
[SuppressMessage("ReSharper", "UnusedType.Global")]
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<NotificationDbContext>
#pragma warning restore CA1515
{
    private string? connectionString;
    private bool initialized;


    public NotificationDbContext CreateDbContext(string[] args)
    {
        Initialize(args);

        var optionsBuilder = new DbContextOptionsBuilder<NotificationDbContext>();
        optionsBuilder
            .UseNpgsql(connectionString, x => x
                .MigrationsAssembly(typeof(Program).Assembly.GetName().Name)
                .MigrationsHistoryTable(HistoryRepository.DefaultTableName, "public"));
        return new NotificationDbContext(optionsBuilder.Options);
    }

    private void Initialize(string[] args)
    {
        if (initialized)
            return;

        var configurationBuilder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", false)
            .AddEnvironmentVariables()
            .AddCommandLine(args);

        var configuration = configurationBuilder.Build();

        connectionString = configuration.GetConnectionString("Default")
                           ?? throw new InvalidOperationException("Missing configured connection string!");

        initialized = true;
    }
}