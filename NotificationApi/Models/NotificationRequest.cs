using System.Diagnostics.CodeAnalysis;
using Common.Enums;

namespace NotificationApi.Models;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global", Justification = "Serialization type")]
[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
internal sealed record NotificationRequest
{
    public required NotificationType Type { get; set;  }
    public required Dictionary<string, object>? Metadata { get; set;  }
}