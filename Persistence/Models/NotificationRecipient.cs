using Common.Enums;

namespace Persistence.Models;

public sealed class NotificationRecipient
{
    public Guid UserId { get; init; }
    public DeliveryChannel DeliveryChannel { get; init; }
    public string DeliveryAddress { get; init; } = string.Empty;
}