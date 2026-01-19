using System.Diagnostics.CodeAnalysis;
using Database.Models.Interfaces;

namespace Database.Models;

[SuppressMessage("Usage", "CA2227:Collection properties should be read only")]
[SuppressMessage("ReSharper", "EntityFramework.ModelValidation.CircularDependency")]
public class Post : ITimeStamped
{
    public required Guid Id { get; init; }
    public required Guid UserId { get; init; }

    public required string Content { get; init; }

    public DateTime CreatedUtc { get; set; }
    public DateTime? ModifiedUtc { get; set; }

    public User User { get; set; } = null!;
    public IList<Comment> Comments { get; set; } = [ ];
    public IList<Reaction> Reactions { get; set; } = [ ];
}