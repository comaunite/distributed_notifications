using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Common.Enums;
using Microsoft.Extensions.Logging;
using NRedisStack.RedisStackCommands;
using Persistence.Entities;
using Persistence.Models;
using Persistence.Stores;
using StackExchange.Redis;

namespace Integrations.Redis.Stores;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public class RedisNotificationStore(INotificationStore inner, IConnectionMultiplexer redis, ILogger<RedisNotificationStore> logger)
    : INotificationStore
{
    private readonly IDatabase db = redis.GetDatabase();

    public async Task<Notification?> CreateAsync(Notification notification, CancellationToken cancellationToken)
    {
        return await inner.CreateAsync(notification, cancellationToken);
    }

    public async Task<IList<DefaultNotificationPreference>> GetDefaultPreferencesAsync(NotificationType type, CancellationToken cancellationToken)
    {
        var cacheKey = $"default_notification_preferences:{type}";

        var cachedResult = await db.JSON().GetAsync<DefaultNotificationPreference[]>(cacheKey);

        if (cachedResult is not null)
        {
            logger.LogInformation("Cache hit for default notification preferences");

            return cachedResult;
        }

        logger.LogInformation("Cache miss for default notification preferences. Fetching from inner store.");

        var result = await inner.GetDefaultPreferencesAsync(type, cancellationToken);

        await db.JSON().SetAsync(cacheKey, "$", result.ToArray());

        return result;
    }

    public async IAsyncEnumerable<NotificationRecipient> GetNotificationRecipientsAsync(NotificationType type, IList<DefaultNotificationPreference> defaults,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var userRecipient in inner.GetNotificationRecipientsAsync(type, defaults, cancellationToken))
        {
            yield return userRecipient;
        }
    }
}