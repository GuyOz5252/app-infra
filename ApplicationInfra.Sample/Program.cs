using ApplicationInfra.Books.Abstract;
using ApplicationInfra.Books.Extensions;
using ApplicationInfra.Books.Http.Extensions;
using ApplicationInfra.Messaging.Abstractions;
using ApplicationInfra.Messaging.Kafka.Extensions;
using ApplicationInfra.Sample;
using ApplicationInfra.Sample.Books;
using ApplicationInfra.Sample.Protobuf;
using ApplicationInfra.Serialization.Extensions;
using ApplicationInfra.Serialization.Protobuf;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddJsonEventSerialization();

builder.Services.AddProtobufEventSerialization(parsers =>
{
    parsers.Add(SampleOrderPlaced.Parser);
});

builder.Services.AddHttpBook<string, ProductConfig, ProductBookLoader>(
    builder.Configuration, "Products");
builder.Services.AddBookRefreshHandler(
    "Products",
    sp => sp.GetRequiredService<ProductValidatorRegistry>());

builder.Services.AddKafka(builder.Configuration, kafka =>
{
    kafka.AddConsumer<SampleOrderPlaced, SampleOrderPlacedEventProcessor, ProtobufEventSerializer>("SampleOrderPlaced");
    kafka.AddProducer<SampleOrderPlaced, ProtobufEventDeserializer>("SampleOrderPlaced");
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

app.MapGet(
    "/products/{id}",
    (string id, [FromKeyedServices("Products")] IBook<string, ProductConfig> products) =>
        products.TryGet(id, out var product)
            ? Results.Ok(product)
            : Results.NotFound());

// Uses the registry directly — no IBook injection needed.
app.MapGet(
    "/products/{id}/validate",
    (string id, decimal price, ProductValidatorRegistry registry) =>
    {
        var validator = registry.GetAll()
            .FirstOrDefault(v => v.ProductId == id);

        if (validator is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(new { productId = id, price, isValid = validator.IsPriceValid(price) });
    });

await app.RunAsync();
