using System.Data;
using System.Runtime.CompilerServices;
using Common.Enums;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Persistence.Entities;
using Persistence.Models;
using Persistence.Stores;

namespace Persistence.Postgres.Stores;

public class PostgresNotificationStore(IDbContextFactory dbContextFactory) : INotificationStore
{
    public async Task<Notification?> CreateAsync(Notification notification, CancellationToken cancellationToken)
    {
        await using var context = dbContextFactory.CreateReadWriteContext();

        await context.AddAsync(notification, cancellationToken);

        await context.SaveChangesAsync(cancellationToken);

        return notification;
    }

    public async IAsyncEnumerable<NotificationRecipient> GetNotificationRecipientsAsync(NotificationType type,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await using var context = dbContextFactory.CreateReadOnlyContext();
        var conn = (NpgsqlConnection)context.Database.GetDbConnection();

        await conn.OpenAsync(cancellationToken);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
                          SELECT
                              u."Id",
                              d."DeliveryChannel",
                              CASE
                                  WHEN d."DeliveryChannel" = 0 THEN u."Email"
                                  WHEN d."DeliveryChannel" = 1 THEN u."PhoneNumber"
                                  ELSE u."DeviceToken"
                              END AS address
                          FROM ntf.default_notification_preferences d
                          CROSS JOIN ntf.users u
                          LEFT JOIN ntf.user_notification_preferences p
                              ON p."UserId" = u."Id"
                             AND p."NotificationType" = d."NotificationType"
                             AND p."DeliveryChannel" = d."DeliveryChannel"
                          WHERE d."NotificationType" = @type
                            AND d."IsEnabled" = true
                            AND (p."IsEnabled" IS NULL OR p."IsEnabled" = true)
                          ORDER BY u."Id";
                          """;

        cmd.Parameters.AddWithValue("type", (int)type);

        // Forward-only streaming reader
        await using var reader =
            await cmd.ExecuteReaderAsync(
                CommandBehavior.SequentialAccess,
                cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            yield return new NotificationRecipient
            {
                UserId = reader.GetGuid(0),
                DeliveryChannel = (DeliveryChannel)reader.GetInt32(1),
                DeliveryAddress = reader.GetString(2)
            };
        }
    }
}