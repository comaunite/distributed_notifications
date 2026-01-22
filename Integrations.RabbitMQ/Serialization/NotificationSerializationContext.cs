using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using Integrations.RabbitMQ.Models;
using Integrations.RabbitMQ.Models.Base;
using Integrations.RabbitMQ.Models.Interfaces;

namespace Integrations.RabbitMQ.Serialization;

/// <summary>
/// Source-generated JSON context for high-performance, reflection-free serialization.
/// This reduces CPU usage and memory allocations significantly at high throughput.
/// </summary>
[JsonSerializable(typeof(BaseNotification))]
[JsonSerializable(typeof(NotifySms))]
[JsonSerializable(typeof(NotifyEmail))]
[JsonSerializable(typeof(NotifyPush))]
[JsonSerializable(typeof(IDeduplicatable))]
[JsonSerializable(typeof(Dictionary<string, object>))]
[JsonSerializable(typeof(JsonElement))]
[JsonSerializable(typeof(object))]
[JsonSourceGenerationOptions(
    WriteIndented = false,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed partial class NotificationSerializationContext : JsonSerializerContext
{

}