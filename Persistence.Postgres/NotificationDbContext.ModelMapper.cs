using Microsoft.EntityFrameworkCore;
using Persistence.Entities;

namespace Persistence.Postgres;

public partial class NotificationDbContext
{
    private static void MapModels(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
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

            entity.HasMany(e => e.Posts)
                  .WithOne(p => p.User)
                  .HasForeignKey(p => p.UserId);

            entity.HasMany(e => e.Comments)
                  .WithOne(c => c.User)
                  .HasForeignKey(c => c.UserId);

            entity.HasMany(e => e.Reactions)
                  .WithOne(r => r.User)
                  .HasForeignKey(r => r.UserId);
        });

        modelBuilder.Entity<Post>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnType("uuid");

            entity.Property(e => e.UserId)
                .IsRequired();

            entity.Property(e => e.Content)
                .IsRequired()
                .HasMaxLength(5000);

            entity.Property(e => e.CreatedUtc)
                .HasDefaultValueSql("now() at time zone 'utc'")
                .IsRequired();

            entity.HasMany(e => e.Comments)
                  .WithOne(c => c.Post)
                  .HasForeignKey(c => c.PostId);

            entity.HasMany(e => e.Reactions)
                  .WithOne(r => r.Post)
                  .HasForeignKey(r => r.PostId);
        });

        modelBuilder.Entity<Comment>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnType("uuid");

            entity.Property(e => e.PostId)
                .IsRequired();

            entity.Property(e => e.UserId)
                .IsRequired();

            entity.Property(e => e.Content)
                .IsRequired()
                .HasMaxLength(2000);

            entity.Property(e => e.CreatedUtc)
                .HasDefaultValueSql("now() at time zone 'utc'")
                .IsRequired();
        });

        modelBuilder.Entity<Reaction>(entity =>
        {
            entity.HasKey(e => new { e.PostId, e.UserId });

            entity.Property(e => e.PostId)
                .IsRequired();

            entity.Property(e => e.UserId)
                .IsRequired();

            entity.Property(e => e.Type)
                .IsRequired();
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnType("uuid");

            entity.Property(e => e.Type)
                .IsRequired();

            entity.Property(e => e.Metadata)
                .HasColumnType("jsonb");

            entity.Property(e => e.CreatedUtc)
                .HasDefaultValueSql("now() at time zone 'utc'")
                .IsRequired();
        });

        modelBuilder.Entity<UserNotificationPreference>(entity =>
        {
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
            entity.HasKey(e => new { e.NotificationType, e.DeliveryChannel });

            entity.Property(e => e.NotificationType)
                .IsRequired();

            entity.Property(e => e.IsEnabled)
                .IsRequired();
        });
    }
}