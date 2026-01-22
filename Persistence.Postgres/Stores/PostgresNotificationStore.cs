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

    public async IAsyncEnumerable<NotificationRecipient> GetNotificationRecipientsAsync(NotificationType type,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var query = from user in readOnlyContext.Users
            from defaultPref in readOnlyContext.DefaultNotificationPreferences
            where defaultPref.IsEnabled && defaultPref.NotificationType == type
            let userPref = readOnlyContext.UserNotificationPreferences
                .FirstOrDefault(p =>
                    p.UserId == user.Id
                    && p.NotificationType == type
                    && p.DeliveryChannel == defaultPref.DeliveryChannel)
            where (userPref == null && defaultPref.IsEnabled) || (userPref != null && userPref.IsEnabled)
            select new
            {
                User = user,
                defaultPref.DeliveryChannel
            }
            into result
            select new NotificationRecipient
            {
                UserId = result.User.Id,
                Email = result.User.Email,
                PhoneNumber = result.User.PhoneNumber,
                DeviceToken = result.User.DeviceToken,
                DeliveryChannel = result.DeliveryChannel
            };

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

            foreach (var recipient in page)
                yield return recipient;
        } while (true);
    }
}