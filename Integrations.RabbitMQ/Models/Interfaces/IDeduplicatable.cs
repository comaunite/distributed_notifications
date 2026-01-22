namespace Integrations.RabbitMQ.Models.Interfaces;

public interface IDeduplicatable
{
    Guid DeduplicationId { get; set; }
}