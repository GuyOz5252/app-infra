using ApplicationInfra.Messaging.Abstractions;
using ApplicationInfra.Messaging.Kafka.MassTransit.Extensions;
using ApplicationInfra.Sample.MassTransit;
using ApplicationInfra.Sample.MassTransit.Protobuf;
using ApplicationInfra.Serialization.Extensions;
using ApplicationInfra.Serialization.Json;
using ApplicationInfra.Serialization.Protobuf;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Registers ProtobufEventSerializer, ProtobufEventDeserializer, and ProtobufMessageParserResolver.
builder.Services.AddProtobufEventSerialization(parsers => parsers.Add(OrderShipped.Parser));

builder.Services.AddMassTransitKafka(builder.Configuration, kafka =>
{
    kafka.AddProducer<OrderPlacedMessage, JsonEventSerializer>("Orders");
    kafka.AddConsumer<OrderPlacedMessage, OrderPlacedConsumer, JsonEventDeserializer>("Orders");

    kafka.AddProducer<OrderShipped, ProtobufEventSerializer>("Shipments");
    kafka.AddConsumer<OrderShipped, OrderShippedConsumer, ProtobufEventDeserializer>("Shipments");
});

var app = builder.Build();

app.MapPost("/orders", async (
    [FromKeyedServices("Orders")] IEventPublisher publisher,
    CancellationToken cancellationToken) =>
{
    var message = new OrderPlacedMessage(Guid.NewGuid(), DateTimeOffset.UtcNow);
    await publisher.PublishAsync(message, cancellationToken).ConfigureAwait(false);
    return Results.Ok(new { message.OrderId, message.PlacedAt });
});

app.MapPost("/shipments", async (
    [FromKeyedServices("Shipments")] IEventPublisher publisher,
    CancellationToken cancellationToken) =>
{
    var message = new OrderShipped
    {
        OrderId = Guid.NewGuid().ToString(),
        TrackingNumber = $"TRACK-{Guid.NewGuid():N}"[..21],
        ShippedAtUnixMillis = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
    };
    await publisher.PublishAsync(message, cancellationToken).ConfigureAwait(false);
    return Results.Ok(new { message.OrderId, message.TrackingNumber });
});

await app.RunAsync();
