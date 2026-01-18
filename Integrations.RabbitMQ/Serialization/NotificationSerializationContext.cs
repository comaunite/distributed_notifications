using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Integrations.RabbitMQ.Models;
using Integrations.RabbitMQ.Models.Base;
using Integrations.RabbitMQ.Models.Interfaces;

namespace Integrations.RabbitMQ.Serialization;

/// <summary>
/// Source-generated JSON context for high-performance, reflection-free serialization.
/// This reduces CPU usage and memory allocations significantly at high throughput.
/// </summary>
[JsonSourceGenerationOptions(
    WriteIndented = false,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    PropertyNamingPolicy = JsonKnownNamingPolicy.Unspecified
)]
[JsonSerializable(typeof(BaseNotification))]
[JsonSerializable(typeof(NotifySms))]
[JsonSerializable(typeof(NotifyEmail))]
[JsonSerializable(typeof(NotifyPush))]
[JsonSerializable(typeof(IDeduplicatable))]
[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed partial class NotificationSerializationContext : JsonSerializerContext
{

}