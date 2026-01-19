using Common.Enums;
using Database.Models.Interfaces;

namespace Database.Models;

public class Notification : ITimeStamped
{
    public required Guid Id { get; init; }
    public required NotificationType Type { get; set; }

    public DateTime CreatedUtc { get; set; }
    public DateTime? ModifiedUtc { get; set; }

    public string? Metadata { get; set; }
}