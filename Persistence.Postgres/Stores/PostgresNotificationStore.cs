using Microsoft.EntityFrameworkCore;
using Persistence.Models.Entities;
using Persistence.Stores;

namespace Persistence.Postgres.Stores;

public class PostgresNotificationStore(NotificationDbContext context, ReadOnlyNotificationDbContext readOnlyContext) : INotificationStore
{
    public async Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await readOnlyContext.Notifications
            .AsNoTracking()
            .FirstOrDefaultAsync(n => n.Id == id, cancellationToken);
    }

    public async Task<Notification?> CreateAsync(Notification notification, CancellationToken cancellationToken)
    {
        await context.AddAsync(notification, cancellationToken);

        return notification;
    }
}