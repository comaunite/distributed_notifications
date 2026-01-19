namespace Database;

public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken ct);
}

public sealed class UnitOfWork(NotificationDbContext dbContext) : IUnitOfWork
{
    public Task SaveChangesAsync(CancellationToken ct) => dbContext.SaveChangesAsync(ct);
}