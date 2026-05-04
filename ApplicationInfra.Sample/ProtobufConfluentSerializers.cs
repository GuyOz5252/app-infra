using Confluent.Kafka;
using Google.Protobuf;

namespace ApplicationInfra.Sample;

internal sealed class ProtobufConfluentSerializer<T> : IAsyncSerializer<T>
    where T : IMessage<T>
{
    public static readonly ProtobufConfluentSerializer<T> Instance = new();

    public Task<byte[]> SerializeAsync(T data, SerializationContext context) =>
        Task.FromResult(data.ToByteArray());
}

internal sealed class ProtobufConfluentDeserializer<T> : IDeserializer<T>
    where T : IMessage<T>
{
    private readonly MessageParser<T> _parser;

    public ProtobufConfluentDeserializer(MessageParser<T> parser)
    {
        _parser = parser;
    }

    public T Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context) =>
        _parser.ParseFrom(data.ToArray());
}
