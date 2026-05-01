using ApplicationInfra.Serialization.Abstract;
using Confluent.Kafka;

namespace ApplicationInfra.Sample.MassTransit.Serialization;

internal sealed class ConfluentSerializerAdapter<T> : ISerializer<T>
{
    private readonly IEventSerializer _inner;

    public ConfluentSerializerAdapter(IEventSerializer inner)
    {
        _inner = inner;
    }

    public byte[] Serialize(T data, SerializationContext context)
    {
        return _inner.Serialize(data);
    }
}
