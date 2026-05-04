using Confluent.Kafka;
using Confluent.Kafka.SyncOverAsync;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using MassTransit;
using ApplicationInfra.Sample.MassTransit;
using ApplicationInfra.Sample.MassTransit.Protobuf;

var builder = WebApplication.CreateBuilder(args);

var bootstrapServers = builder.Configuration["Kafka:BootstrapServers"] ?? "localhost:9092";
var ordersTopic = builder.Configuration["Kafka:OrdersTopic"] ?? "mt-orders";
var ordersGroup = builder.Configuration["Kafka:OrdersConsumerGroup"] ?? "mt-sample-orders-group";
var shipmentsTopic = builder.Configuration["Kafka:ShipmentsTopic"] ?? "mt-shipments";
var shipmentsGroup = builder.Configuration["Kafka:ShipmentsConsumerGroup"] ?? "mt-sample-shipments-group";
var schemaRegistryUrl = builder.Configuration["SchemaRegistry:Url"] ?? "http://localhost:8081";

builder.Services.AddSingleton<ISchemaRegistryClient>(_ =>
    new CachedSchemaRegistryClient(new SchemaRegistryConfig { Url = schemaRegistryUrl }));

builder.Services.AddMassTransit(x =>
{
    x.AddRider(rider =>
    {
        rider.AddProducer<OrderPlacedMessage>(ordersTopic);

        rider.AddProducer<OrderShipped>(shipmentsTopic,
            new ProducerConfig { BootstrapServers = bootstrapServers },
            (riderCtx, p) => p.SetValueSerializer(
                new ProtobufSerializer<OrderShipped>(riderCtx.GetRequiredService<ISchemaRegistryClient>())
                    .AsSyncOverAsync()));

        rider.AddConsumer<OrderPlacedConsumer>();
        rider.AddConsumer<OrderShippedConsumer>();

        rider.UsingKafka((context, k) =>
        {
            k.Host(bootstrapServers);

            k.TopicEndpoint<OrderPlacedMessage>(ordersTopic, ordersGroup, e =>
            {
                e.ConfigureConsumer<OrderPlacedConsumer>(context);
            });

            k.TopicEndpoint<OrderShipped>(shipmentsTopic, shipmentsGroup, e =>
            {
                e.SetValueDeserializer(new ProtobufDeserializer<OrderShipped>().AsSyncOverAsync());
                e.ConfigureConsumer<OrderShippedConsumer>(context);
            });
        });
    });
});

var app = builder.Build();

app.MapPost("/orders", async (ITopicProducer<OrderPlacedMessage> producer, CancellationToken ct) =>
{
    var message = new OrderPlacedMessage(Guid.NewGuid(), DateTimeOffset.UtcNow);
    await producer.Produce(message, ct).ConfigureAwait(false);
    return Results.Ok(new { message.OrderId, message.PlacedAt });
});

app.MapPost("/shipments", async (ITopicProducer<OrderShipped> producer, CancellationToken ct) =>
{
    var message = new OrderShipped
    {
        OrderId = Guid.NewGuid().ToString(),
        TrackingNumber = $"TRACK-{Guid.NewGuid():N}"[..21],
        ShippedAtUnixMillis = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
    };
    await producer.Produce(message, ct).ConfigureAwait(false);
    return Results.Ok(new { message.OrderId, message.TrackingNumber });
});

await app.RunAsync();
