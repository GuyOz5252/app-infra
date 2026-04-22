using Google.Protobuf;

namespace AppInfra.Serialization.Protobuf;

public sealed class ProtobufMessageParserResolverBuilder
{
    private readonly Dictionary<Type, MessageParser> _parsers = [];

    public ProtobufMessageParserResolverBuilder Add<T>(MessageParser<T> parser)
        where T : IMessage<T>
    {
        ArgumentNullException.ThrowIfNull(parser);
        var messageType = typeof(T);
        return !_parsers.TryAdd(messageType, parser)
            ? throw new InvalidOperationException($"A protobuf parser for {messageType.FullName} is already registered.")
            : this;
    }

    internal ProtobufMessageParserResolver Build()
    {
        return new ProtobufMessageParserResolver(_parsers);
    }
}
