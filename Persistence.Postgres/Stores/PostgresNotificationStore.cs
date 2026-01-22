using System.Runtime.CompilerServices;
using Common.Enums;
using Microsoft.EntityFrameworkCore;
using Persistence.Entities;
using Persistence.Models;
using Persistence.Stores;

namespace Persistence.Postgres.Stores;

public class PostgresNotificationStore(NotificationDbContext context, ReadOnlyNotificationDbContext readOnlyContext) : INotificationStore
{
    public async Task<Notification?> CreateAsync(Notification notification, CancellationToken cancellationToken)
    {
        await context.AddAsync(notification, cancellationToken);

        await context.SaveChangesAsync(cancellationToken);

        return notification;
    }

    public async Task<IList<DefaultNotificationPreference>> GetDefaultPreferencesAsync(NotificationType type, CancellationToken cancellationToken)
    {
        return await readOnlyContext.DefaultNotificationPreferences
            .Where(p => p.NotificationType == type)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async IAsyncEnumerable<NotificationRecipient> GetNotificationRecipientsAsync(NotificationType type, IList<DefaultNotificationPreference> defaults,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var query = readOnlyContext.Users
            .Select(u => new NotificationRecipient
            {
                UserId = u.Id,
                Email = u.Email,
                PhoneNumber = u.PhoneNumber,
                DeviceToken = u.DeviceToken,
                EnabledDeliveryChannels = defaults
                    .AsEnumerable()
                    .Where(d =>
                        d.IsEnabled
                        || u.NotificationPreferences
                            .Any(np => np.NotificationType == d.NotificationType && np.IsEnabled))
                    .Select(d => d.DeliveryChannel)
            })
            .Where(u => u.EnabledDeliveryChannels.Any())
            .AsNoTracking();

        var offset = 0;
        do
        {
            var page = await query
                .Skip(offset)
                .Take(500)
                .AsNoTracking()
                .ToArrayAsync(cancellationToken);

            if (page.Length == 0)
                yield break;

            offset += page.Length;

            foreach (var instruction in page)
                yield return instruction;
        } while (true);
    }
}