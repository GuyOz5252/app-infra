using Confluent.Kafka;
using Google.Protobuf;

namespace ApplicationInfra.Messaging.Kafka.MassTransit.Serialization;

public class ConfluentProtobufDeserializer<T> : IDeserializer<T>
    where T : IMessage<T>, new()
{
    private static readonly MessageParser<T> Parser = new(() => new T());
    
    public T Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context)
    {
        return Parser.ParseFrom(data);
    }
}
