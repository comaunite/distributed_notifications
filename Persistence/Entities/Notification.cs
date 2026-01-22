using System.Diagnostics.CodeAnalysis;
using Common.Enums;
using Persistence.Entities.Interfaces;

namespace Persistence.Entities;

public class Notification : ITimeStamped
{
    public required Guid Id { get; init; } = Guid.CreateVersion7();
    public required NotificationType Type { get; set; }

    public DateTime CreatedUtc { get; set; }
    public DateTime? ModifiedUtc { get; set; }

    [SuppressMessage("Usage", "CA2227:Collection properties should be read only")]
    public Dictionary<string, object>? Metadata { get; set; }
}