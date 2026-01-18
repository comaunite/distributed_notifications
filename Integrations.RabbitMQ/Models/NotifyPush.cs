using Integrations.RabbitMQ.Models.Base;
using Integrations.RabbitMQ.Models.Interfaces;

namespace Integrations.RabbitMQ.Models;

public record NotifyPush : BaseNotification, IDeduplicatable
{
    public required Guid DeduplicationId { get; init; }
    public required string DeviceToken { get; init; }
}