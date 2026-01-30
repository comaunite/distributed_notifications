// See https://aka.ms/new-console-template for more information

using Common.Enums;
using Hosting.Runtime.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Persistence.Entities;
using Persistence.Postgres;
using Persistence.Postgres.HostingExtensions;

var builder = Host.CreateApplicationBuilder();

builder.AddConsoleLogging();

builder.AddPostgresDatabase();

using var app = builder.Build();

Console.WriteLine("Starting test data population...");

var dbContextFactory = app.Services.GetRequiredService<IDbContextFactory>();

await using var dbContext = dbContextFactory.CreateReadWriteContext();

Console.WriteLine("Populating test data...");

// Create Default Notification Settings if they do not exist
if (!dbContext.DefaultNotificationPreferences.Any())
{
    Console.WriteLine("Adding default notification preferences...");

    dbContext.DefaultNotificationPreferences.AddRange(new[]
    {
        new DefaultNotificationPreference
        {
            NotificationType = NotificationType.NewPost,
            DeliveryChannel = DeliveryChannel.Email,
            IsEnabled = true
        },
        new DefaultNotificationPreference
        {
            NotificationType = NotificationType.NewPost,
            DeliveryChannel = DeliveryChannel.Sms,
            IsEnabled = false
        },
        new DefaultNotificationPreference
        {
            NotificationType = NotificationType.NewPost,
            DeliveryChannel = DeliveryChannel.Push,
            IsEnabled = true
        },
    });

    await dbContext.SaveChangesAsync();
}

// Add users if they do not exist
if (!dbContext.Users.Any())
{
    Console.WriteLine("Adding users...");

    for (var i = 1; i <= 10000; i++)
    {
        dbContext.Users.Add(new User
        {
            Id = Guid.CreateVersion7(),
            Username = $"user{i}",
            Email = $"user{i}@test.com",
            PhoneNumber = $"+100000000{i:D4}",
            DeviceToken = $"device_token_{i}",
        });
    }

    await dbContext.SaveChangesAsync();
}

// Add user notification preferences if they do not exist
if (!dbContext.UserNotificationPreferences.Any())
{
    // TODO: Add logic to populate user notification preferences
}

Console.WriteLine("Test data population completed.");