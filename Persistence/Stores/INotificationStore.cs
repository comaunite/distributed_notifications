using Common.Enums;
using Persistence.Entities;
using Persistence.Models;

namespace Persistence.Stores;

// In the real-world would probably split read and write stores for separation of dependencies,
// e.g. for applications that only require read access to the datastore.
public interface INotificationStore
{
    Task<Notification?> CreateAsync(Notification notification, CancellationToken cancellationToken);
    IAsyncEnumerable<NotificationRecipient> GetNotificationRecipientsAsync(NotificationType type, CancellationToken cancellationToken);
}