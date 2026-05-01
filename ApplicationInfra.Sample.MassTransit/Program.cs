using ApplicationInfra.Sample.MassTransit;
using ApplicationInfra.Sample.MassTransit.Protobuf;
using ApplicationInfra.Sample.MassTransit.Serialization;
using ApplicationInfra.Serialization.Extensions;
using ApplicationInfra.Serialization.Protobuf;
using MassTransit;

var builder = WebApplication.CreateBuilder(args);

var bootstrapServers = builder.Configuration["Kafka:BootstrapServers"] ?? "localhost:9092";
var ordersTopic = builder.Configuration["Kafka:Topics:Orders"] ?? "mt-orders";
var shipmentsTopic = builder.Configuration["Kafka:Topics:Shipments"] ?? "mt-shipments";
var ordersGroup = builder.Configuration["Kafka:ConsumerGroups:Orders"] ?? "mt-sample-orders-group";
var shipmentsGroup = builder.Configuration["Kafka:ConsumerGroups:Shipments"] ?? "mt-sample-shipments-group";

// Registers ProtobufEventSerializer, ProtobufEventDeserializer, and
// ProtobufMessageParserResolver as singletons in the DI container.
builder.Services.AddProtobufEventSerialization(parsers => parsers.Add(OrderShipped.Parser));

builder.Services.AddMassTransit(x =>
{
    x.UsingInMemory();

    x.AddRider(rider =>
    {
        // --- Register consumers ---
        rider.AddConsumer<OrderPlacedConsumer>();
        rider.AddConsumer<OrderShippedConsumer>();

        // --- Register producers (one per topic) ---
        // Both callbacks receive a live IRiderRegistrationContext (the built service provider)
        // because UsingKafka and AddProducer callbacks are invoked after the DI container is built.
        rider.AddProducer<OrderPlacedMessage>(ordersTopic);
        rider.AddProducer<OrderShipped>(shipmentsTopic, (riderContext, p) =>
            p.SetValueSerializer(new ConfluentSerializerAdapter<OrderShipped>(
                riderContext.GetRequiredService<ProtobufEventSerializer>())));

        rider.UsingKafka((context, k) =>
        {
            k.Host(bootstrapServers);

            k.TopicEndpoint<OrderPlacedMessage>(ordersTopic, ordersGroup, e =>
            {
                e.ConfigureConsumer<OrderPlacedConsumer>(context);
            });

            k.TopicEndpoint<OrderShipped>(shipmentsTopic, shipmentsGroup, e =>
            {
                e.SetValueDeserializer(new ConfluentDeserializerAdapter<OrderShipped>(
                    context.GetRequiredService<ProtobufEventDeserializer>()));
                e.ConfigureConsumer<OrderShippedConsumer>(context);
            });
        });
    });
});

var app = builder.Build();

app.MapPost("/orders", async (ITopicProducer<OrderPlacedMessage> producer, CancellationToken cancellationToken) =>
{
    var message = new OrderPlacedMessage(Guid.NewGuid(), DateTimeOffset.UtcNow);
    await producer.Produce(message, cancellationToken).ConfigureAwait(false);
    return Results.Ok(new { message.OrderId, message.PlacedAt });
});

app.MapPost("/shipments", async (ITopicProducer<OrderShipped> producer, CancellationToken cancellationToken) =>
{
    var message = new OrderShipped
    {
        OrderId = Guid.NewGuid().ToString(),
        TrackingNumber = $"TRACK-{Guid.NewGuid():N}"[..21],
        ShippedAtUnixMillis = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
    };
    await producer.Produce(message, cancellationToken).ConfigureAwait(false);
    return Results.Ok(new { message.OrderId, message.TrackingNumber });
});

await app.RunAsync();
