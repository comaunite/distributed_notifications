using Database;

namespace NotificationOrchestrator.Models;

internal sealed record UserNotificationPreferences(
    Guid UserId,
    string? Email,
    string? PhoneNumber,
    string? DeviceToken,
    IList<DeliveryChannel> PreferredChannels);