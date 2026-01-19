using Integrations.RabbitMQ;
using Integrations.RabbitMQ.Factories;
using Integrations.RabbitMQ.Models;
using Integrations.RabbitMQ.Publishers;
using NotificationApi.Models;
using Persistence.Models.Entities;
using Persistence.Postgres;
using Persistence.Stores;

namespace NotificationApi.Services;

internal interface INotificationService
{
    Task<NotificationResponse> PostNotificationAsync(NotificationRequest request, CancellationToken cancellationToken);
}

internal sealed class NotificationService(IUnitOfWork uow, INotificationStore notificationStore, IRabbitMqConnectionFactory connectionFactory) : INotificationService
{
    public async Task<NotificationResponse> PostNotificationAsync(NotificationRequest request, CancellationToken cancellationToken)
    {
        var notification = new Notification
        {
            Id = Guid.CreateVersion7(),
            Type = request.Type,
            Metadata = request.Content
        };

        await notificationStore.CreateAsync(notification, cancellationToken);
        await uow.SaveChangesAsync(cancellationToken);

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        await using var channel = await connection.CreateChannelAsync(null, cancellationToken);

        var publisher = new RabbitMqPublisher(channel, Constants.Exchange);

        var message = new NotificationCreated
        {
            NotificationId = notification.Id,
            Type = request.Type,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var result = await publisher.PublishAsync(message, notification.Id.ToString(), cancellationToken);

        return new NotificationResponse
        {
            Success = result.success,
            ErrorMessage = !result.success
                ? result.errorMessage ?? "Unknown error occurred while publishing notification"
                : null,
            CorrelationId = notification.Id,
        };
    }
}