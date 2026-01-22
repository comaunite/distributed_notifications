using Common.Enums;

namespace Persistence.Models;

public sealed class NotificationRecipient
{
    public Guid UserId { get; init; }
    public string? Email { get; init; }
    public string? PhoneNumber { get; init; }
    public string? DeviceToken { get; init; }
    public DeliveryChannel DeliveryChannel { get; init; }
}