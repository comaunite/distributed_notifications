using System.Text.Json.Serialization;

namespace Persistence.Serialization;

[JsonSerializable(typeof(Models.NotificationRecipient))]
public partial class NotificationRecipientSerializationContext : JsonSerializerContext
{

}