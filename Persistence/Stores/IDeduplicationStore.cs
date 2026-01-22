namespace Persistence.Stores;

public interface IDeduplicationStore
{
    Task<bool> IsDuplicateAsync(string deduplicationId);
    Task MarkAsProcessedAsync(string deduplicationId);
}