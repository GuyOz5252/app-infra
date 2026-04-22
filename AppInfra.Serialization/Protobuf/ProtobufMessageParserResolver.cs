using Google.Protobuf;

namespace AppInfra.Serialization.Protobuf;

public sealed class ProtobufMessageParserResolver
{
    private readonly Dictionary<Type, MessageParser> _parsers;

    internal ProtobufMessageParserResolver(Dictionary<Type, MessageParser> parsers)
    {
        _parsers = new Dictionary<Type, MessageParser>(parsers);
    }

    public MessageParser Resolve(Type messageType)
    {
        if (!_parsers.TryGetValue(messageType, out var parser))
        {
            throw new InvalidOperationException(
                $"No protobuf parser registered for {messageType.FullName}. " +
                $"Register it with {nameof(ProtobufMessageParserResolverBuilder)}.{nameof(ProtobufMessageParserResolverBuilder.Add)} when configuring protobuf serialization.");
        }

        return parser;
    }
}
