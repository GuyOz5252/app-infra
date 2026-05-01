namespace ApplicationInfra.Sample.MassTransit;

public sealed record OrderPlacedMessage(Guid OrderId, DateTimeOffset PlacedAt);
