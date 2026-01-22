using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Common.Enums;

namespace Integrations.RabbitMQ.Models.Base;

[JsonDerivedType(typeof(NotificationCreated), typeDiscriminator: "created")]
[JsonDerivedType(typeof(NotifyEmail), typeDiscriminator: "email")]
[JsonDerivedType(typeof(NotifySms), typeDiscriminator: "sms")]
[JsonDerivedType(typeof(NotifyPush), typeDiscriminator: "push")]
[SuppressMessage("Usage", "CA2227:Collection properties should be read only")]
public record BaseNotification
{
    public Guid NotificationId { get; set; }
    public NotificationType Type { get; set; }
    public Dictionary<string, object>? Metadata { get; set; } = [ ];
    public DateTimeOffset CreatedAt { get; set; }
}