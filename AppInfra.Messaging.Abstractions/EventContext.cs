namespace AppInfra.Messaging.Abstractions;

public record EventContext(
    string Topic,
    string? Key,
    IReadOnlyDictionary<string, string> Headers,
    int? Partition,
    long? Offset);
