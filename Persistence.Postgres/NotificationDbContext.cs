using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Persistence.Models.Entities;

namespace Persistence.Postgres;

/// <summary>
/// I realize that postgres isn't the optimal choice for a database with the proposed number of posts, comments, users, and notifications.
/// But for demo purposes it will do just fine. In real life, all things considered, it's likely that a NoSQL database would be a better fit.
/// Likely that it would itself raise notifications into queues on new records being created.
///
/// Ofcourse the model also uses some naive assumptions about relationships between entities.
/// E.g., a Reaction is only linked to a Post; All Users receive notifications for all Posts (unless unsubscribed); etc.
/// </summary>
/// <param name="options"></param>
public partial class NotificationDbContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<User> Users { get; init; }
    public DbSet<Post> Posts { get; init; }
    public DbSet<Comment> Comments { get; init; }
    public DbSet<Reaction> Reactions { get; init; }
    public DbSet<UserNotificationPreference> UserNotificationPreferences { get; init; }
    public DbSet<DefaultNotificationPreference> DefaultNotificationPreferences { get; init; }
    public DbSet<Notification> Notifications { get; init; }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        ArgumentNullException.ThrowIfNull(configurationBuilder);

        // We don't want all the ID properties of all of our entities to default to auto-increments.
        // If we want that behavior, we'll explicitly state so below, not have it by default.
        configurationBuilder.Conventions.Remove<ValueGenerationConvention>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        base.OnModelCreating(modelBuilder);

        MapModels(modelBuilder);
    }
}