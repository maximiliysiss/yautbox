using System.Text.Json;
using System.Text.Json.Serialization;
using Yautbox.Entities;

namespace Yautbox.Options;

public sealed class OutboxSerializerOptions
{
    public JsonSerializerOptions JsonSerializerOptions { get; } = new JsonSerializerOptions()
        .AddJsonConverter<JsonStringEnumConverter>()
        .AddJsonIdentifierConverter<OutboxMessageId>();
}
