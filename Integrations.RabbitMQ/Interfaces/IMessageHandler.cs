namespace Integrations.RabbitMQ.Interfaces;

public interface IMessageHandler
{
    string QueueName { get; }

    ValueTask<(bool success, string? error)> ProcessAsync(ReadOnlyMemory<byte> body, string? correlationId, CancellationToken cancellationToken);
}