using Integrations.RabbitMQ.Models.Base;

namespace Integrations.RabbitMQ.Models;

public record NotifyEmail : BaseNotification
{
    public NotifyEmail()
    {

    }

    public NotifyEmail(BaseNotification message) : base(
        message.NotificationId,
        message.Type,
        message.Metadata,
        message.CreatedAt)
    {

    }
}