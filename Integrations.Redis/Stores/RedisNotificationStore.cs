using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.Json;
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
        // Cache to be invalidated when defaults change for a specific notification type (Very Rare)
        // Individual entries within a set can be invalidated when a user updates their notification preferences or delivery address (Somewhat Common)

        var setKey = $"recipients:{type}";

        if (await db.KeyExistsAsync(setKey))
        {
            logger.LogDebug("Streaming cached recipients for {Type}", type);

            await foreach (var entry in db.SetScanAsync(setKey).WithCancellation(cancellationToken))
            {
                if (cancellationToken.IsCancellationRequested)
                    yield break;

                var recipient = JsonSerializer.Deserialize<NotificationRecipient>(entry.ToString());
                if (recipient != null)
                {
                    yield return recipient;
                }
            }

            yield break;
        }

        logger.LogInformation("Fetching notification recipients for notification type {NotificationType} from inner store", type);

        await foreach (var userRecipient in inner.GetNotificationRecipientsAsync(type, cancellationToken))
        {
            var serialized = JsonSerializer.Serialize(userRecipient);
            await db.SetAddAsync(setKey, serialized);

            yield return userRecipient;
        }
    }
}