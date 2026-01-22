namespace Integrations.RabbitMQ.Models.Interfaces;

public interface IDeliverableNotification
{
    string DeliveryAddress { get; set; }
}