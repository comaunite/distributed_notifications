using System.Diagnostics.CodeAnalysis;
using Database.Models.Interfaces;

namespace Database.Models;

[SuppressMessage("Usage", "CA2227:Collection properties should be read only")]
[SuppressMessage("ReSharper", "EntityFramework.ModelValidation.CircularDependency")]
public class User : ITimeStamped
{
    public required Guid Id { get; init; }
    public required string Username { get; init; }
    public required string Email { get; set; }
    public string? PhoneNumber { get; set; }

    public DateTime CreatedUtc { get; set; }
    public DateTime? ModifiedUtc { get; set; }

    public IList<Post> Posts { get; set; } = [];
    public IList<Comment> Comments { get; set; } = [];
    public IList<Reaction> Reactions { get; set; } = [];
    public IList<UserNotificationPreference> NotificationPreferences { get; set; } = [];
}