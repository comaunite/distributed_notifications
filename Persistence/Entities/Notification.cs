using System.Diagnostics.CodeAnalysis;
using Common.Enums;
using Persistence.Entities.Interfaces;

namespace Persistence.Entities;

/// <summary>
/// In the real world this would be stored in a NoSQL database, but for the sake of this example we are storing it in Postgres as a JSONB column.
/// </summary>
public class Notification : ITimeStamped
{
    public required Guid Id { get; init; } = Guid.CreateVersion7();
    public required NotificationType Type { get; set; }

    public DateTime CreatedUtc { get; set; }
    public DateTime? ModifiedUtc { get; set; }

    [SuppressMessage("Usage", "CA2227:Collection properties should be read only")]
    public Dictionary<string, object>? Metadata { get; set; }
}