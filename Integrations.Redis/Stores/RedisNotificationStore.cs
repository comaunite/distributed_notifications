using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Common.Enums;
using Microsoft.Extensions.Logging;
using Persistence.Entities;
using Persistence.Models;
using Persistence.Stores;
using StackExchange.Redis;

namespace Integrations.Redis.Stores;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public class RedisNotificationStore(INotificationStore inner, IConnectionMultiplexer redis, ILogger<RedisNotificationStore> logger)
    : INotificationStore
{
    [SuppressMessage("Performance", "CA1823:Avoid unused private fields")]
    private readonly IDatabase db = redis.GetDatabase();

    public async Task<Notification?> CreateAsync(Notification notification, CancellationToken cancellationToken)
    {
        return await inner.CreateAsync(notification, cancellationToken);
    }

    public async IAsyncEnumerable<NotificationRecipient> GetNotificationRecipientsAsync(NotificationType type,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        logger.LogInformation("Fetching notification recipients for notification type {NotificationType} from inner store", type);

        await foreach (var userRecipient in inner.GetNotificationRecipientsAsync(type, cancellationToken))
        {
            yield return userRecipient;
        }
    }
}