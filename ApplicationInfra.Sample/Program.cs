using ApplicationInfra.Messaging.Abstractions;
using ApplicationInfra.Messaging.Kafka.MassTransit.Extensions;
using ApplicationInfra.Sample;
using ApplicationInfra.Sample.Protobuf;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMassTransitKafka(builder.Configuration, cfg =>
{
    cfg.AddProducer<OrderPlacedEvent>("Example");

    cfg.AddProducer<SampleOrderPlaced>("ProtoExample", (_, p) =>
        p.SetValueSerializer(ProtobufConfluentSerializer<SampleOrderPlaced>.Instance));

    cfg.AddConsumer<OrderPlacedEvent, OrderPlacedConsumerProcessor>("Orders");

    cfg.AddConsumer<SampleOrderPlaced, SampleOrderPlacedConsumerProcessor>("ProtoOrders", e =>
        e.SetValueDeserializer(new ProtobufConfluentDeserializer<SampleOrderPlaced>(SampleOrderPlaced.Parser)));
});

var app = builder.Build();

app.MapPost(
    "/publish-example",
    async ([FromKeyedServices("Example")] IEventPublisher publisher, CancellationToken cancellationToken) =>
    {
        var metadata = new PublishMetadata(
            Key: Guid.NewGuid().ToString("N"),
            Headers: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["event-type"] = nameof(OrderPlacedEvent),
            });

        await publisher
            .PublishAsync(
                new OrderPlacedEvent(Guid.NewGuid(), DateTimeOffset.UtcNow),
                metadata,
                cancellationToken)
            .ConfigureAwait(false);
        return Results.Ok();
    });

app.MapPost(
    "/publish-proto-example",
    async ([FromKeyedServices("ProtoExample")] IEventPublisher publisher, CancellationToken cancellationToken) =>
    {
        var metadata = new PublishMetadata(
            Key: Guid.NewGuid().ToString("N"),
            Headers: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["event-type"] = nameof(SampleOrderPlaced),
            });

        var message = new SampleOrderPlaced
        {
            OrderId = Guid.NewGuid().ToString(),
            PlacedAtUnixMillis = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
        };

        await publisher.PublishAsync(message, metadata, cancellationToken).ConfigureAwait(false);
        return Results.Ok();
    });

await app.RunAsync();
