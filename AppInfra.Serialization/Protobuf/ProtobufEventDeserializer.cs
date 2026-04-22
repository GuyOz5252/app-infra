using AppInfra.Serialization.Abstract;

namespace AppInfra.Serialization.Protobuf;

public sealed class ProtobufEventDeserializer : IEventDeserializer
{
    private readonly ProtobufMessageParserResolver _protobufMessageParserResolver;

    public ProtobufEventDeserializer(ProtobufMessageParserResolver protobufMessageParserResolver)
    {
        _protobufMessageParserResolver = protobufMessageParserResolver;
    }

    public TEvent Deserialize<TEvent>(ReadOnlyMemory<byte> data)
    {
        var parser = _protobufMessageParserResolver.Resolve(typeof(TEvent));
        var message = parser.ParseFrom(data.Span);
        return (TEvent)message;
    }
}
