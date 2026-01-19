using Common.Enums;
using Persistence.Models.Entities.Interfaces;

namespace Persistence.Models.Entities;

public class Notification : ITimeStamped
{
    public required Guid Id { get; init; } = Guid.CreateVersion7();
    public required NotificationType Type { get; set; }

    public DateTime CreatedUtc { get; set; }
    public DateTime? ModifiedUtc { get; set; }

    public string? Metadata { get; set; }
}