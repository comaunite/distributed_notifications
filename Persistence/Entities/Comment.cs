using Persistence.Entities.Interfaces;

namespace Persistence.Entities;

public class Comment : ITimeStamped
{
    public required Guid Id { get; init; } = Guid.CreateVersion7();
    public required Guid PostId { get; init; }
    public required Guid UserId { get; init; }

    public required string Content { get; init; }

    public DateTime CreatedUtc { get; set; }
    public DateTime? ModifiedUtc { get; set; }

    public Post Post { get; set; } = null!;
    public User User { get; set; } = null!;
}