using Common;

namespace Integrations.RabbitMQ.Models.Base;

public record BaseNotification
{
    public required Guid NotificationId { get; init; }
    public required NotificationType Type { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
}