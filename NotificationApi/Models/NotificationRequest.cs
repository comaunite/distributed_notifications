using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Common;

namespace NotificationApi.Models;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global", Justification = "Serialization type")]
internal sealed record NotificationRequest
{
    public required NotificationType Type { get; init;  }

    [MaxLength(200)]
    public required string Content { get; init;  }
}