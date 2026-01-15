using Common;

namespace NotificationApi.Models;

public record NotificationRequest
{
    public required NotificationType NotificationType { get; init;  }
}