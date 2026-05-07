using ApplicationInfra.Books.Abstract;
using ApplicationInfra.Books.Http.Extensions;
using ApplicationInfra.Messaging.Abstractions;
using ApplicationInfra.Messaging.Kafka.Extensions;
using ApplicationInfra.Sample;
using ApplicationInfra.Sample.Books;
using ApplicationInfra.Sample.Protobuf;
using ApplicationInfra.Serialization.Extensions;
using ApplicationInfra.Serialization.Json;
using ApplicationInfra.Serialization.Protobuf;

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

// Books — hot config loaded once at startup, refreshed in the background.
// URL and other options come from Books:Products in appsettings.json.
// Inject as [FromKeyedServices("Products")] IBook<string, ProductConfig>.
builder.Services.AddHttpBook<string, ProductConfig, ProductBookLoader>(
    builder.Configuration, "Products");

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

app.MapGet(
    "/products/{id}",
    (string id, [FromKeyedServices("Products")] IBook<string, ProductConfig> products) =>
        products.TryGet(id, out var product)
            ? Results.Ok(product)
            : Results.NotFound());

await app.RunAsync();
