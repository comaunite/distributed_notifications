using Common.Enums;

namespace Persistence.Entities;

/// <summary>
/// Only store the difference with default notification preferences
/// </summary>
public class UserNotificationPreference
{
    public required Guid UserId { get; init; }
    public required NotificationType NotificationType { get; init; }
    public required DeliveryChannel DeliveryChannel { get; init; }
    public required bool IsEnabled { get; init; }

    public User User { get; set; } = null!;
}