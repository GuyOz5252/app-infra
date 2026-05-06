using Confluent.Kafka;
using Google.Protobuf;

namespace ApplicationInfra.Messaging.Kafka.MassTransit.Serialization;

public class ConfluentProtobufSerializer<T> : ISerializer<T>
    where T : IMessage<T>
{
    public byte[] Serialize(T data, SerializationContext context)
    {
        return data.ToByteArray();
    }
}
