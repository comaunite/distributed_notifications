using System.Diagnostics.CodeAnalysis;

namespace Integrations.RabbitMQ.Extensions;

[SuppressMessage("ReSharper", "ConvertToExtensionBlock")]
public static class HeaderExtensions
{
    public static string GetString(this IDictionary<string, object> headers, string key)
    {
        if (headers.TryGetValue(key, out var value))
        {
            return value switch
            {
                string str => str,
                byte[] bytes => System.Text.Encoding.UTF8.GetString(bytes),
                _ => throw new InvalidOperationException($"Invalid string header type: {key}")
            };
        }

        throw new InvalidOperationException($"Missing string header: {key}");
    }
}