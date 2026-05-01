using ApplicationInfra.Serialization.Abstract;
using Confluent.Kafka;

namespace ApplicationInfra.Messaging.Kafka.MassTransit.Serialization;

internal sealed class ConfluentDeserializerAdapter<T> : IDeserializer<T>
{
    private readonly IEventDeserializer _inner;

    public ConfluentDeserializerAdapter(IEventDeserializer inner)
    {
        _inner = inner;
    }

    public T Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context)
    {
        return _inner.Deserialize<T>(new ReadOnlyMemory<byte>(data.ToArray()));
    }
}
