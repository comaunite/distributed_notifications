using Integrations.RabbitMQ.Models.Base;
using Integrations.RabbitMQ.Models.Interfaces;

namespace Integrations.RabbitMQ.Models;

public record NotifySms(string DeliveryAddress) : BaseNotification, IDeduplicatable, IDeliverableNotification
{
    public Guid DeduplicationId { get; set; }
    public string DeliveryAddress { get; set; } = DeliveryAddress;
}