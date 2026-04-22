# ApplicationInfra.Serialization

JSON and Protobuf implementations of `IEventSerializer` / `IEventDeserializer` for use with messaging packages.

## Abstractions

| Interface | Description |
|---|---|
| `IEventSerializer` | Serializes an event to `byte[]` |
| `IEventDeserializer` | Deserializes `ReadOnlyMemory<byte>` into a typed event |

## Implementations

| Class | Format |
|---|---|
| `JsonEventSerializer` / `JsonEventDeserializer` | System.Text.Json |
| `ProtobufEventSerializer` / `ProtobufEventDeserializer` | Google.Protobuf |

## Registration

```csharp
// JSON
builder.Services.AddJsonEventSerialization();

// Protobuf — register a parser for each message type consumed
builder.Services.AddProtobufEventSerialization(parsers =>
{
    parsers.Add(MyProtoMessage.Parser);
});
```

Both methods register their serializer and deserializer as singletons using `TryAdd`, so they are safe to call multiple times.
