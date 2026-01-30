using Microsoft.Extensions.Options;
using Persistence.Postgres.HostingExtensions;

namespace Persistence.Postgres;

public interface IDbContextFactory
{
    NotificationDbContext CreateReadOnlyContext();
    NotificationDbContext CreateReadWriteContext();
}

public class NotificationDbContextFactory(IOptions<NotificationDbContextFactoryOptions> options) : IDbContextFactory
{
    public NotificationDbContext CreateReadOnlyContext()
    {
        if (string.IsNullOrEmpty(options.Value.ReadOnlyConnectionString))
            throw new InvalidOperationException("Read-only connection string is not set!");

        return CreateInternal(options.Value.ReadOnlyConnectionString);
    }

    public NotificationDbContext CreateReadWriteContext()
    {
        if (string.IsNullOrEmpty(options.Value.ReadWriteConnectionString))
            throw new InvalidOperationException("Read-write connection string is not set!");

        return CreateInternal(options.Value.ReadWriteConnectionString);
    }

    private static NotificationDbContext CreateInternal(string connectionString)
    {
        return NotificationDbContext.Create(connectionString);
    }
}