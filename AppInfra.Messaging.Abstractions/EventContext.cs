namespace AppInfra.Messaging.Abstractions;

public sealed record EventContext(
    string? Key,
    IReadOnlyDictionary<string, string> Headers,
    IReadOnlyDictionary<string, string?> Attributes);
