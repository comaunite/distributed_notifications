using Integrations.RabbitMQ.Models.Base;
using Integrations.RabbitMQ.Models.Interfaces;

namespace Integrations.RabbitMQ.Models;

public record NotifySms : BaseNotification, IDeduplicatable
{
    public required Guid DeduplicationId { get; init; }
    public required string PhoneNumber { get; init; }
}