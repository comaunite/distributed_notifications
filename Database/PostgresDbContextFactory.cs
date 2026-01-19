using Database.Interfaces;

namespace Database;

public class PostgresDbContextFactory : IDbContextFactory
{
    private string? readOnlyConnectionString;
    private string? readWriteConnectionString;

    public NotificationDbContext CreateReadOnlyContext()
    {
        return CreateInternal(true);
    }

    public NotificationDbContext CreateReadWriteContext()
    {
        return CreateInternal(false);
    }

    private NotificationDbContext CreateInternal(bool readOnly)
    {
        EnsureConnectionStrings();

        if (readOnly && readOnlyConnectionString is null)
        {
            throw new InvalidOperationException("Read-only connection string is not configured.");
        }

        if (!readOnly && readWriteConnectionString is null)
        {
            throw new InvalidOperationException("Read-write connection string is not configured.");
        }

        // Sensitive logging is enabled for demo purposes.
        // Otherwise, should be enabled based on environment
        return NotificationDbContext.CreateWithPostgres(readOnly
                ? readOnlyConnectionString!
                : readWriteConnectionString!,
            true);
    }

    /// <summary>
    /// This is a demo implementation.
    /// The real world should use a secret manager or service role-based access to the database.
    /// </summary>
    private void EnsureConnectionStrings()
    {
        readOnlyConnectionString ??= Environment.GetEnvironmentVariable("ConnectionStrings__Database");
        readWriteConnectionString ??= Environment.GetEnvironmentVariable("ConnectionStrings__Database");
    }
}