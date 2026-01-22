namespace Persistence.Entities;

public class Reaction
{
    public required Guid PostId { get; init; }
    public required Guid UserId { get; init; }
    public required ReactionType Type { get; init; }

    public User User { get; set; } = null!;
    public Post Post { get; set; } = null!;
}

public enum ReactionType
{
    Dislike = 0,
    Like = 1
}