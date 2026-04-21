using System.Text.Json;
using AppInfra.Serialization.Abstract;

namespace AppInfra.Serialization.Json;

public sealed class JsonEventDeserializer : IEventDeserializer
{
    private readonly JsonSerializerOptions _options;

    public JsonEventDeserializer(JsonSerializerOptions? options = null)
    {
        _options = options ?? new JsonSerializerOptions(JsonSerializerDefaults.Web);
    }

    public TEvent Deserialize<TEvent>(ReadOnlyMemory<byte> data)
    {
        var value = JsonSerializer.Deserialize<TEvent>(data.Span, _options);
        if (value is null && default(TEvent) is null)
        {
            throw new JsonException("Deserialized event is null.");
        }

        return value!;
    }
}
