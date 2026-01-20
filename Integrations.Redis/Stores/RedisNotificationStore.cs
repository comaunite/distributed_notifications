using Persistence.Models.Entities;
using Persistence.Stores;

namespace Integrations.Redis.Stores;

public class RedisNotificationStore(INotificationStore inner) : INotificationStore
{
    public async Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        // Redis caching logic would go here

        return await inner.GetByIdAsync(id, cancellationToken);
    }

    public async Task<Notification?> CreateAsync(Notification notification, CancellationToken cancellationToken)
    {
        var createdNotification = await inner.CreateAsync(notification, cancellationToken);

        // Redis caching logic would go here

        return createdNotification;
    }
}