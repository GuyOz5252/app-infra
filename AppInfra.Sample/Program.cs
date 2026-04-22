using AppInfra.Messaging.Kafka.Extensions;
using AppInfra.Messaging.Abstractions;
using AppInfra.Sample;
using AppInfra.Sample.Protos;
using AppInfra.Serialization.Extensions;
using AppInfra.Serialization.Json;
using AppInfra.Serialization.Protobuf;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddJsonEventSerialization();

builder.Services.AddProtobufEventSerialization(parsers =>
{
    parsers.Add(SampleOrderPlaced.Parser);
});

builder.Services.AddKafkaProducer<JsonEventSerializer>(builder.Configuration, "Example");
builder.Services.AddKafkaProducer<ProtobufEventSerializer>(builder.Configuration, "ProtoExample");

builder.Services.AddKafkaConsumer<OrderPlacedEvent, OrderPlacedConsumerProcessor, JsonEventDeserializer>(
    builder.Configuration, "Orders");

builder.Services.AddKafkaConsumer<SampleOrderPlaced, SampleOrderPlacedConsumerProcessor, ProtobufEventDeserializer>(
    builder.Configuration, "ProtoOrders");

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
