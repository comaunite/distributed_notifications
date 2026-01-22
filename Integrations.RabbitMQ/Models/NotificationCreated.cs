using System.Diagnostics.CodeAnalysis;
using Integrations.RabbitMQ.Models.Base;

namespace Integrations.RabbitMQ.Models;

[SuppressMessage("Usage", "CA2227:Collection properties should be read only")]
public sealed record NotificationCreated : BaseNotification
{
    
}