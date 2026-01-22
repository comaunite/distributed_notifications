using Common.Enums;
using Persistence.Entities;
using Persistence.Models;

namespace Persistence.Stores;

public interface INotificationStore
{
    Task<Notification?> CreateAsync(Notification notification, CancellationToken cancellationToken);


    Task<IList<DefaultNotificationPreference>> GetDefaultPreferencesAsync(NotificationType type,CancellationToken cancellationToken);

    IAsyncEnumerable<NotificationRecipient> GetNotificationRecipientsAsync(NotificationType type, IList<DefaultNotificationPreference> defaults,
        CancellationToken cancellationToken);
}