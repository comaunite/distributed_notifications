using Persistence.Models.Entities;

namespace Persistence.Stores.Interfaces;

public interface INotificationStore
{
    Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<Notification?> CreateAsync(Notification notification, CancellationToken cancellationToken);
}