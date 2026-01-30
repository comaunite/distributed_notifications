using Integrations.RabbitMQ.Models.Base;

namespace Integrations.RabbitMQ.Models;

public record NotifyPush : BaseNotification
{
    public NotifyPush()
    {

    }

    public NotifyPush(BaseNotification message) : base(
        message.NotificationId,
        message.Type,
        message.Metadata,
        message.CreatedAt)
    {

    }
}