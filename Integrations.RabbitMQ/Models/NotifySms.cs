using Integrations.RabbitMQ.Models.Base;

namespace Integrations.RabbitMQ.Models;

public record NotifySms : BaseNotification
{
    public NotifySms()
    {

    }

    public NotifySms(BaseNotification message) : base(
        message.NotificationId,
        message.Type,
        message.Metadata,
        message.CreatedAt)
    {

    }
}