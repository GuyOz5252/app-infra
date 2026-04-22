# ApplicationInfra.Messaging.Kafka

Kafka producer and consumer implementations backed by [Confluent.Kafka](https://github.com/confluentinc/confluent-kafka-dotnet). Depends on `ApplicationInfra.Messaging.Abstractions` and `ApplicationInfra.Serialization`.

## Producer

`KafkaProducer<TSerializer>` implements `IEventPublisher`. It is keyed in the DI container by name, so multiple producers with different topics or serializers can coexist.

### Registration

```csharp
// Registers a named producer that serializes events as JSON
services.AddKafkaProducer<JsonEventSerializer>(configuration, "Orders");
```

Configuration is read from `Kafka:Producer:{name}` in `appsettings.json`:

```json
{
  "Kafka": {
    "Producer": {
      "Orders": {
        "BootstrapServers": "localhost:9092",
        "Topic": "orders",
        "Username": "user",
        "Password": "pass"
      }
    }
  }
}
```

### Usage

```csharp
app.MapPost("/orders", async (
    [FromKeyedServices("Orders")] IEventPublisher publisher,
    CancellationToken ct) =>
{
    var metadata = new PublishMetadata(Key: Guid.NewGuid().ToString("N"));
    await publisher.PublishAsync(new OrderPlacedEvent(...), metadata, ct);
    return Results.Ok();
});
```

## Consumer

`KafkaConsumerHostedService<TEvent, THandler, TDeserializer>` is a `BackgroundService` that subscribes to a topic and dispatches each message to an `IEventProcessor<TEvent>` in a fresh DI scope.

### Registration

```csharp
services.AddKafkaConsumer<OrderPlacedEvent, OrderPlacedProcessor, JsonEventDeserializer>(
    configuration, "Orders");
```

Configuration is read from `Kafka:Consumers:{name}`:

```json
{
  "Kafka": {
    "Consumers": {
      "Orders": {
        "BootstrapServers": "localhost:9092",
        "Topic": "orders",
        "Username": "consumer-group",
        "Password": "pass",
        "AutoOffsetReset": "Earliest"
      }
    }
  }
}
```

### Implementing a processor

```csharp
public class OrderPlacedProcessor : IEventProcessor<OrderPlacedEvent>
{
    public async Task ProcessEventAsync(
        OrderPlacedEvent @event,
        EventContext context,
        CancellationToken cancellationToken)
    {
        // handle event
    }
}
```

## Security

Both producer and consumer connect over SASL/SCRAM-SHA-256 with `SASL_PLAINTEXT`.
