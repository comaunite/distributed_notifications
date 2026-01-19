using Common.Enums;

namespace Persistence.Models.Entities;

public class DefaultNotificationPreference
{
    public required int NotificationType { get; init; }
    public required DeliveryChannel DeliveryChannel { get; init; }
    public required bool IsEnabled { get; init; }
}

