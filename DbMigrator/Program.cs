using Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json")
    .AddEnvironmentVariables()
    .Build();

using var loggerFactory = LoggerFactory.Create(logBuilder =>
{
    logBuilder.AddConsole();
});

var logger = loggerFactory.CreateLogger("DbMigrator");

try
{
    var connectionString = Environment.GetEnvironmentVariable("connectionString")
                           ?? configuration["connectionString"];
    
    var optionsBuilder = new DbContextOptionsBuilder<NotificationDbContext>();
    optionsBuilder
        .UseNpgsql(connectionString,
            x => x
                .MigrationsAssembly(typeof(Program).Assembly.GetName().Name)
                .MigrationsHistoryTable(HistoryRepository.DefaultTableName, "public"));


    await using var dbContext = new NotificationDbContext(optionsBuilder.Options);

    var pending = (await dbContext.Database.GetPendingMigrationsAsync()).ToArray();
    var allMigrations = dbContext.Database.GetMigrations().ToArray();

    logger.LogInformation("{AllMigrationsLength}/{Length} migrations already applied.",
        allMigrations.Length - pending.Length, allMigrations.Length);

    try
    {
        if (pending.Length == 0)
        {
            logger.LogInformation("No migrations pending.");
        }
        else
        {
            logger.LogInformation("Pending migrations: \n {Name}", string.Join("\n ", pending));
            logger.LogInformation("Applying migrations...");

            await dbContext.Database.MigrateAsync();

            logger.LogInformation("Migrations applied.");
        }
    }
#pragma warning disable CA1031
    catch (Exception error)
#pragma warning restore CA1031
    {
        logger.LogError(error, "Error applying migrations: {ErrorMessage}", error.Message);
    }
}
catch (Exception error)
{
    Console.WriteLine("UNHANDLED EXCEPTION: " + error);
    throw;
}