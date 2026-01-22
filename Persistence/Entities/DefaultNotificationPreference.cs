using Common.Enums;

namespace Persistence.Entities;

public class DefaultNotificationPreference
{
    public required NotificationType NotificationType { get; init; }
    public required DeliveryChannel DeliveryChannel { get; init; }
    public required bool IsEnabled { get; init; }
}

