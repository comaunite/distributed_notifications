using System.Diagnostics.CodeAnalysis;
using Persistence.Entities.Interfaces;

namespace Persistence.Entities;

[SuppressMessage("Usage", "CA2227:Collection properties should be read only")]
[SuppressMessage("ReSharper", "EntityFramework.ModelValidation.CircularDependency")]
public class Post : ITimeStamped
{
    public required Guid Id { get; init; } = Guid.CreateVersion7();
    public required Guid UserId { get; init; }

    public required string Content { get; init; }

    public DateTime CreatedUtc { get; set; }
    public DateTime? ModifiedUtc { get; set; }

    public User User { get; set; } = null!;
    public IList<Comment> Comments { get; set; } = [ ];
    public IList<Reaction> Reactions { get; set; } = [ ];
}