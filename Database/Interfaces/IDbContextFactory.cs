namespace Database.Interfaces;

public interface IDbContextFactory
{
    NotificationDbContext CreateReadOnlyContext();
    NotificationDbContext CreateReadWriteContext();
}