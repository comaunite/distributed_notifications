using Integrations.RabbitMQ;
using Integrations.RabbitMQ.Models;
using NotificationApi.Models;

namespace NotificationApi.Services;

internal interface INotificationService
{
    Task<Result> PostNotificationAsync(NotificationRequest request, CancellationToken ct);
}

internal sealed class NotificationService(IRabbitMqConnectionFactory connectionFactory) : INotificationService
{
    public async Task<Result> PostNotificationAsync(NotificationRequest request, CancellationToken ct)
    {
        await using var connection = await connectionFactory.CreateConnectionAsync(ct);
        await using var channel = await connection.CreateChannelAsync(null, ct);

        var publisher = new RabbitMqPublisher(channel, Constants.Exchange);

        var result = await publisher.PublishAsync(new NotificationCreated
        {
            NotificationId = Guid.NewGuid(),
            Type =  request.Type,
            CreatedAt = DateTimeOffset.UtcNow
        }, ct);

        return result.success
            ? Result.Success()
            : Result.Failure(result.errorMessage ?? "Unknown error occurred while publishing notification");
    }
}