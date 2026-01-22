using Persistence.Stores;
using StackExchange.Redis;

namespace Integrations.Redis.Stores;

public class RedisDeduplicationStore(IConnectionMultiplexer redis) : IDeduplicationStore
{
    private readonly IDatabase db = redis.GetDatabase();

    public async Task<bool> IsDuplicateAsync(string deduplicationId)
    {
        var key = $"dedup:{deduplicationId}";

        return await db.KeyExistsAsync(key);
    }

    public async Task MarkAsProcessedAsync(string deduplicationId)
    {
        var key = $"dedup:{deduplicationId}";

        await db.StringSetAsync(key, "1", TimeSpan.FromHours(1), When.NotExists);
    }
}