using Microsoft.EntityFrameworkCore;
using Persistence.Entities;

namespace Persistence.Postgres;

public partial class NotificationDbContext
{
    private static void MapModels(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users", "ntf");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnType("uuid");

            entity.Property(e => e.Username)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Email)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(20);

            entity.Property(e => e.CreatedUtc)
                .HasDefaultValueSql("now() at time zone 'utc'")
                .IsRequired();

            entity.HasMany(e => e.NotificationPreferences)
                  .WithOne(np => np.User)
                  .HasForeignKey(np => np.UserId);
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.ToTable("notifications", "ntf");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnType("uuid");

            entity.Property(e => e.Type)
                .IsRequired();

            entity.Property(e => e.Metadata)
                .HasColumnType("jsonb")
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v),
                    v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(v));

            entity.Property(e => e.CreatedUtc)
                .HasDefaultValueSql("now() at time zone 'utc'")
                .IsRequired();
        });

        modelBuilder.Entity<UserNotificationPreference>(entity =>
        {
            entity.ToTable("user_notification_preferences", "ntf");

            entity.HasKey(e => new { e.UserId, e.NotificationType, e.DeliveryChannel });

            entity.Property(e => e.UserId)
                .IsRequired();

            entity.Property(e => e.NotificationType)
                .IsRequired();

            entity.Property(e => e.IsEnabled)
                .IsRequired();
        });

        modelBuilder.Entity<DefaultNotificationPreference>(entity =>
        {
            entity.ToTable("default_notification_preferences", "ntf");

            entity.HasKey(e => new { e.NotificationType, e.DeliveryChannel });

            entity.Property(e => e.NotificationType)
                .IsRequired();

            entity.Property(e => e.IsEnabled)
                .IsRequired();
        });
    }
}