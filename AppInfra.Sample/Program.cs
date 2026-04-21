using AppInfra.Kafka.Abstract;
using AppInfra.Kafka.Extensions;
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
    async ([FromKeyedServices("Example")] IKafkaProducer producer, CancellationToken cancellationToken) =>
    {
        await producer.ProduceAsync(
                new OrderPlacedEvent(Guid.NewGuid(), DateTimeOffset.UtcNow),
                cancellationToken)
            .ConfigureAwait(false);
        return Results.Ok();
    });

await app.RunAsync();
