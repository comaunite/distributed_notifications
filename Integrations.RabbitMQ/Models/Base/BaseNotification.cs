using System.Text.Json.Serialization;
using Common.Enums;

namespace Integrations.RabbitMQ.Models.Base;

[JsonDerivedType(typeof(NotificationCreated), typeDiscriminator: "created")]
[JsonDerivedType(typeof(NotifyEmail), typeDiscriminator: "email")]
[JsonDerivedType(typeof(NotifySms), typeDiscriminator: "sms")]
[JsonDerivedType(typeof(NotifyPush), typeDiscriminator: "push")]
public record BaseNotification
{
    public required Guid NotificationId { get; init; }
    public required NotificationType Type { get; init; }
    public required Dictionary<string, object>? Metadata { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
}