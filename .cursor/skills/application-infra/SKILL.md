---
name: application-infra
description: Full context for the ApplicationInfra NuGet library (.NET 10, GitHub Packages). Covers Kafka messaging (IEventPublisher/IEventProcessor), JSON/Protobuf serialization, and minimal-API endpoint auto-registration (IEndpoint). Use when developing new features or packages inside this repo, or when consuming ApplicationInfra packages in another service.
---

# ApplicationInfra

Multi-package .NET 10 library published to GitHub Packages.
Source: [github.com/GuyOz5252/application-infra](https://github.com/GuyOz5252/application-infra)
Solution file: `ApplicationInfra.slnx` (new Visual Studio format — there is no `.sln` file).

## Packages

| NuGet ID | Purpose |
|---|---|
| `ApplicationInfra.Messaging.Abstractions` | Core contracts: `IEventPublisher`, `IEventProcessor<T>`, `EventContext`, `PublishMetadata` |
| `ApplicationInfra.Messaging.Kafka` | Kafka producer + long-running consumer (requires Abstractions + Serialization) |
| `ApplicationInfra.Serialization` | JSON and Protobuf serializers/deserializers + DI extensions |
| `ApplicationInfra.Api` | Minimal-API endpoint auto-registration via `IEndpoint` |

> `ApplicationInfra.Messaging.Kafka.MassTransit` is an **empty placeholder** — do not consume or publish it until it has a real implementation.
> `ApplicationInfra.Sample` is the functional demo; it has `GeneratePackageOnBuild = false` and is never published.

---

## Using the Packages in Another Service

For a complete usage reference see [usage.md](usage.md).

### 1. Add the GitHub Packages feed

```xml
<!-- nuget.config -->
<configuration>
  <packageSources>
    <add key="application-infra"
         value="https://nuget.pkg.github.com/GuyOz5252/index.json" />
  </packageSources>
</configuration>
```

### 2. Kafka Messaging

**appsettings.json** config contract:

```json
{
  "Kafka": {
    "Producer": {
      "<Name>": { "BootstrapServers": "", "Topic": "", "Username": "", "Password": "" }
    },
    "Consumers": {
      "<Name>": { "BootstrapServers": "", "Topic": "", "Username": "", "Password": "" }
    }
  }
}
```

**Program.cs:**

```csharp
// Pick one or both serialization strategies
builder.Services.AddJsonEventSerialization();
builder.Services.AddProtobufEventSerialization(parsers => parsers.Add(MyProtoEvent.Parser));

// Keyed producer (name must match appsettings key)
builder.Services.AddKafkaProducer<JsonEventSerializer>(builder.Configuration, "MyProducer");

// Consumer (registers a BackgroundService automatically)
builder.Services.AddKafkaConsumer<MyEvent, MyEventProcessor, JsonEventDeserializer>(
    builder.Configuration, "MyConsumer");
```

**Implement `IEventProcessor<T>`:**

```csharp
internal sealed class MyEventProcessor : IEventProcessor<MyEvent>
{
    private readonly ILogger<MyEventProcessor> _logger;

    public MyEventProcessor(ILogger<MyEventProcessor> logger)
    {
        _logger = logger;
    }

    public Task ProcessEventAsync(MyEvent @event, EventContext context, CancellationToken cancellationToken)
    {
        context.Attributes.TryGetValue(KafkaEventContextAttributes.Partition, out var partition);
        // process the event
        return Task.CompletedTask;
    }
}
```

**Inject `IEventPublisher` (keyed):**

```csharp
app.MapPost("/publish", async (
    [FromKeyedServices("MyProducer")] IEventPublisher publisher,
    CancellationToken ct) =>
{
    var metadata = new PublishMetadata(
        Key: Guid.NewGuid().ToString("N"),
        Headers: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["event-type"] = nameof(MyEvent),
        });

    await publisher.PublishAsync(new MyEvent(...), metadata, ct).ConfigureAwait(false);
    return Results.Ok();
});
```

### 3. Minimal API Endpoints

```csharp
// Program.cs
builder.Services.AddEndpoints(Assembly.GetExecutingAssembly());
app.MapEndpoints(); // optional: app.MapEndpoints(routeGroupBuilder)
```

```csharp
// MyEndpoint.cs
internal sealed class MyEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/my-route", () => Results.Ok());
    }
}
```

---

## Developing the Library

For build, code-style rules, and publishing details see [developing.md](developing.md).

### Quick facts

- **Target framework:** `net10.0`
- **Nullable + ImplicitUsings:** enabled on all projects
- **All warnings are errors** (`TreatWarningsAsErrors`, `EnforceCodeStyleInBuild`)
- **SonarAnalyzer.CSharp** runs as a build-time analyzer
- Shared build defaults live in `Directory.Build.props`; central package versions in `Directory.Packages.props` (CPM enabled)
- `PackageVersion` is **not** in source — it is injected by CI at pack time
- No unit test projects exist; `ApplicationInfra.Sample` is the manual verification target
