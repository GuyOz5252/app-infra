# ApplicationInfra.Sample

Runnable reference application demonstrating how to wire up the ApplicationInfra packages together in a minimal API host.

## What it covers

- JSON and Protobuf serialization registration
- Multiple named Kafka producers (one per serialization format)
- Multiple named Kafka consumers with dedicated `IEventProcessor<T>` implementations
- Publishing events via HTTP endpoints

## Endpoints

| Method | Path | Description |
|---|---|---|
| `POST` | `/publish-example` | Publishes an `OrderPlacedEvent` as JSON |
| `POST` | `/publish-proto-example` | Publishes a `SampleOrderPlaced` Protobuf message |

## Running locally

Update `appsettings.json` (or user secrets) with your Kafka connection details under `Kafka:Producer` and `Kafka:Consumers`, then:

```bash
dotnet run
```
