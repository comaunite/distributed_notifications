using RabbitMQ.Client;

namespace Integrations.RabbitMQ.Interfaces;

public interface IPublishingMessageHandler
{
    void InitPublisher(IChannel channel);
}