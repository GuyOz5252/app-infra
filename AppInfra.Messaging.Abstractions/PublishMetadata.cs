namespace AppInfra.Messaging.Abstractions;

public sealed record PublishMetadata(string? Key = null, IReadOnlyDictionary<string, string>? Headers = null);
