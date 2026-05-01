# ApplicationInfra — Consumer Usage Reference

## NuGet Feed

GitHub Packages: `https://nuget.pkg.github.com/GuyOz5252/index.json`

A personal access token (PAT) with `read:packages` scope is required to restore from GitHub Packages.

```xml
<!-- nuget.config -->
<configuration>
  <packageSources>
    <add key="application-infra"
         value="https://nuget.pkg.github.com/GuyOz5252/index.json" />
  </packageSources>
  <packageSourceCredentials>
    <application-infra>
      <add key="Username" value="YOUR_GITHUB_USERNAME" />
      <add key="ClearTextPassword" value="YOUR_PAT" />
    </application-infra>
  </packageSourceCredentials>
</configuration>
```

---

## ApplicationInfra.Messaging.Abstractions

The base contract package. All other messaging packages depend on it. Reference it directly only when you need the abstractions without a concrete transport.

### Key types

| Type | Role |
|------|------|
| `IEventPublisher` | Publish events; use `[FromKeyedServices("name")]` to inject |
| `IEventProcessor<TEvent>` | Implement to handle consumed events |
| `EventContext` | Passed to `ProcessEventAsync`; carries `Key`, `Headers`, `Attributes` |
| `PublishMetadata` | Optional; supply `Key` and/or `Headers` when publishing |

---

## ApplicationInfra.Messaging.Kafka

Depends on: `ApplicationInfra.Messaging.Abstractions`, `ApplicationInfra.Serialization`, `Confluent.Kafka`

### Configuration

Both producers and consumers are configured from `appsettings.json` (or any `IConfiguration` source).

```json
{
  "Kafka": {
    "Producer": {
      "<Name>": {
        "BootstrapServers": "broker:9092",
        "Topic": "my-topic",
        "Username": "producer-user",
        "Password": "secret"
      }
    },
    "Consumers": {
      "<Name>": {
        "BootstrapServers": "broker:9092",
        "Topic": "my-topic",
        "Username": "consumer-user",
        "Password": "secret",
        "AutoOffsetReset": "Earliest"
      }
    }
  }
}
```

`AutoOffsetReset` is the Confluent `AutoOffsetReset` enum name (`Earliest` / `Latest` / `Error`).

### Producer registration

```csharp
// Registers IEventPublisher as a keyed singleton under "MyProducer"
builder.Services.AddKafkaProducer<JsonEventSerializer>(builder.Configuration, "MyProducer");
builder.Services.AddKafkaProducer<ProtobufEventSerializer>(builder.Configuration, "ProtoProducer");
```

Multiple producers with different names and/or serializers can coexist in one service.

### Consumer registration

```csharp
// Registers a BackgroundService that drives the consume loop
builder.Services.AddKafkaConsumer<MyEvent, MyEventProcessor, JsonEventDeserializer>(
    builder.Configuration, "MyConsumer");
```

The processor (`MyEventProcessor`) is resolved as a **keyed scoped** service per consume loop iteration — a new scope is created for each message.

### Implementing IEventProcessor\<T>

```csharp
// Must be internal sealed (minimal visibility)
internal sealed class MyEventProcessor : IEventProcessor<MyEvent>
{
    private readonly ILogger<MyEventProcessor> _logger; // ILogger first
    private readonly IMyDependency _dep;

    public MyEventProcessor(ILogger<MyEventProcessor> logger, IMyDependency dep)
    {
        _logger = logger;
        _dep = dep;
    }

    public async Task ProcessEventAsync(
        MyEvent @event,
        EventContext context,
        CancellationToken cancellationToken)
    {
        // Read Kafka-specific attributes
        context.Attributes.TryGetValue(KafkaEventContextAttributes.Partition, out var partition);
        context.Attributes.TryGetValue(KafkaEventContextAttributes.Offset, out var offset);

        await _dep.DoWorkAsync(@event, cancellationToken);
    }
}
```

### Publishing events

Inject `IEventPublisher` via `[FromKeyedServices("name")]`:

```csharp
// Minimal API endpoint
app.MapPost("/orders", async (
    [FromKeyedServices("MyProducer")] IEventPublisher publisher,
    CreateOrderRequest request,
    CancellationToken ct) =>
{
    var metadata = new PublishMetadata(
        Key: request.OrderId.ToString("N"),
        Headers: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["event-type"] = nameof(OrderPlacedEvent),
        });

    await publisher.PublishAsync(new OrderPlacedEvent(request.OrderId, DateTimeOffset.UtcNow), metadata, ct)
        .ConfigureAwait(false);

    return Results.Ok();
});
```

In a service, use constructor injection with `[FromKeyedServices]` attribute or `IKeyedServiceProvider`.

### Consumer runtime behaviour

- The consumer loop runs in a `BackgroundService` for the lifetime of the host.
- `ConsumeException` is logged and the loop continues (no crash on transient errors).
- The offset is committed after the processor returns successfully.
- Processor exceptions are logged; `OperationCanceledException` is ignored (clean shutdown).
- Each message is processed in a **new DI scope** — scoped services are safe to use in processors.

---

## ApplicationInfra.Serialization

### JSON

```csharp
builder.Services.AddJsonEventSerialization();
```

Registers `JsonEventSerializer` and `JsonEventDeserializer` as singletons.

```csharp
// Consume
builder.Services.AddKafkaConsumer<MyEvent, MyProcessor, JsonEventDeserializer>(...);

// Produce
builder.Services.AddKafkaProducer<JsonEventSerializer>(...);
```

### Protobuf

Events must be generated from `.proto` files using `Grpc.Tools` and implement `Google.Protobuf.IMessage<T>`.

```csharp
builder.Services.AddProtobufEventSerialization(parsers =>
{
    parsers.Add(MyProtoEvent.Parser);
    parsers.Add(AnotherProtoEvent.Parser);
});
```

```csharp
builder.Services.AddKafkaConsumer<MyProtoEvent, MyProtoProcessor, ProtobufEventDeserializer>(...);
builder.Services.AddKafkaProducer<ProtobufEventSerializer>(...);
```

Each proto message type must be registered with its parser before use. Attempting to deserialize an unregistered type throws `InvalidOperationException`.

---

## ApplicationInfra.Api

Auto-discovers and registers all `IEndpoint` implementations in a given assembly.

### Registration

```csharp
// Program.cs
builder.Services.AddEndpoints(Assembly.GetExecutingAssembly());

var app = builder.Build();
app.MapEndpoints();
// or: app.MapEndpoints(app.MapGroup("/api").WithOpenApi());
```

### Implementing IEndpoint

```csharp
// One class per file; internal sealed
internal sealed class CreateOrderEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/orders", HandleAsync);
    }

    private static async Task<IResult> HandleAsync(
        CreateOrderRequest request,
        IOrderService orderService,
        CancellationToken ct)
    {
        var result = await orderService.CreateAsync(request, ct);
        return Results.Created($"/orders/{result.Id}", result);
    }
}
```

Endpoints are registered as `Transient` and resolved at `MapEndpoints()` call time. Any dependencies needed inside `MapEndpoint` can be injected into the constructor, but minimal-API handler parameters come from the DI container automatically.
