using AppInfra.Kafka.Extensions;
using AppInfra.Messaging.Abstractions;
using AppInfra.Sample;
using AppInfra.Serialization.Extensions;
using AppInfra.Serialization.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddJsonEventSerialization();

builder.Services.AddKafkaProducer<JsonEventSerializer>(builder.Configuration, "Example");

builder.Services.AddKafkaConsumer<OrderPlacedEvent, OrderPlacedConsumerProcessor, JsonEventDeserializer>(
    builder.Configuration, "Orders");

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

await app.RunAsync();
