using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Persistence.Models.Entities;
using Persistence.Stores;

namespace Integrations.Redis.Stores;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public class RedisNotificationStore(INotificationStore inner, ILogger<RedisNotificationStore> logger) : INotificationStore
{
    public async Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        // Redis caching logic would go here

        logger.LogInformation("Rolling through cache to fetch notification ID {NotificationId}", id);

        return await inner.GetByIdAsync(id, cancellationToken);
    }

    public async Task<Notification?> CreateAsync(Notification notification, CancellationToken cancellationToken)
    {
        var createdNotification = await inner.CreateAsync(notification, cancellationToken);

        logger.LogInformation("Rolling notification with ID {NotificationId} into cache", createdNotification!.Id);

        // Redis caching logic would go here

        return createdNotification;
    }
}