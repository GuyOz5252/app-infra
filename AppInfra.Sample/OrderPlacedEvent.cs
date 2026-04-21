namespace AppInfra.Sample;

public sealed record OrderPlacedEvent(Guid OrderId, DateTimeOffset PlacedAt);
