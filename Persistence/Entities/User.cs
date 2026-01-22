using System.Diagnostics.CodeAnalysis;
using Persistence.Entities.Interfaces;

namespace Persistence.Entities;

[SuppressMessage("Usage", "CA2227:Collection properties should be read only")]
[SuppressMessage("ReSharper", "EntityFramework.ModelValidation.CircularDependency")]
public class User : ITimeStamped
{
    public required Guid Id { get; init; } = Guid.CreateVersion7();
    public required string Username { get; init; }
    public required string Email { get; set; }
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// In real life this would be a collection to support multiple devices per user
    /// </summary>
    public string? DeviceToken { get; set; }

    public DateTime CreatedUtc { get; set; }
    public DateTime? ModifiedUtc { get; set; }

    public IList<Post> Posts { get; set; } = [];
    public IList<Comment> Comments { get; set; } = [];
    public IList<Reaction> Reactions { get; set; } = [];
    public IList<UserNotificationPreference> NotificationPreferences { get; set; } = [];
}