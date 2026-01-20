using Integrations.RabbitMQ;
using Integrations.RabbitMQ.Models;
using NotificationApi.Models;
using Persistence.Models.Entities;
using Persistence.Stores;

namespace NotificationApi.Services;

internal interface INotificationService
{
    Task<NotificationResponse> PostNotificationAsync(NotificationRequest request, CancellationToken cancellationToken);
}

internal sealed class NotificationService(INotificationStore notificationStore, IRabbitMqPublisher publisher,
    ILogger<NotificationService> logger)
    : INotificationService
{
    public async Task<NotificationResponse> PostNotificationAsync(NotificationRequest request, CancellationToken cancellationToken)
    {
        var notification = await SaveNotificationAsync(request, cancellationToken);

        var result = await PublishToRabbitMqAsync(notification, cancellationToken);

        return new NotificationResponse
        {
            Success = result.success,
            ErrorMessage = !result.success
                ? result.errorMessage ?? "Unknown error occurred while publishing notification"
                : null,
            CorrelationId = notification.Id,
        };
    }

    private async Task<(bool success, string? errorMessage)> PublishToRabbitMqAsync(Notification notification, CancellationToken cancellationToken)
    {
        logger.LogInformation("Publishing notification {NotificationId} to RabbitMQ", notification.Id);

        var message = new NotificationCreated
        {
            NotificationId = notification.Id,
            Type = notification.Type,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var result = await publisher.PublishAsync(message, notification.Id.ToString(), cancellationToken);

        return result;
    }

    private async Task<Notification> SaveNotificationAsync(NotificationRequest request, CancellationToken cancellationToken)
    {
        var notification = new Notification
        {
            Id = Guid.CreateVersion7(),
            Type = request.Type,
            Metadata = request.Content
        };

        logger.LogInformation("Saving notification {NotificationId} to database", notification.Id);

        await notificationStore.CreateAsync(notification, cancellationToken);

        return notification;
    }
}