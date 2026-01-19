using Persistence.Models.Entities;
using Persistence.Stores.Interfaces;

namespace Persistence.Stores.Postgres;

public class PostgresNotificationStore : INotificationStore
{
    public Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<Notification?> CreateAsync(Notification notification, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}