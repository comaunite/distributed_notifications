using Integrations.RabbitMQ;
using NotificationApi.Models;

namespace NotificationApi.Services;

public interface INotificationService
{
    Task PostNotificationAsync(NotificationRequest request, CancellationToken ct);
}

public class NotificationService(IRabbitMqConnectionFactory rabbitMqConnectionFactory) : INotificationService
{
    public async Task PostNotificationAsync(NotificationRequest request, CancellationToken ct)
    {
        await using var connection = await rabbitMqConnectionFactory.CreateConnectionAsync(ct);
        await using var channel = await connection.CreateChannelAsync(null, ct);

        // var publisher = new RabbitMqPublisher(channel, Constants.Exchange);
        // await publisher.PublishAsync(request, ct);
    }
}