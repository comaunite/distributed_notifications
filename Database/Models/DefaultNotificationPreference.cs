using Common.Enums;

namespace Database.Models;

public class DefaultNotificationPreference
{
    public required int NotificationType { get; init; }
    public required DeliveryChannel DeliveryChannel { get; init; }
    public required bool IsEnabled { get; init; }
}

