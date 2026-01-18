using System.Diagnostics.CodeAnalysis;

namespace NotificationApi.Models;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global", Justification = "Serialization type")]
internal sealed record NotificationResponse : Result
{
    public Guid CorrelationId { get; init; }
}