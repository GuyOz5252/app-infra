# ApplicationInfra.Messaging.Abstractions

Transport-agnostic contracts for event publishing and processing. All other messaging packages depend on these abstractions.

## Types

### `IEventPublisher`

Publishes events to a message broker.

```csharp
Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default);
Task PublishAsync<TEvent>(TEvent @event, PublishMetadata? metadata, CancellationToken cancellationToken = default);
```

### `IEventProcessor<TEvent>`

Handles a single event type consumed from a message broker.

```csharp
Task ProcessEventAsync(TEvent @event, EventContext context, CancellationToken cancellationToken);
```

### `EventContext`

Immutable context passed to `IEventProcessor<TEvent>` on consume, containing the message key, headers, and transport-specific attributes.

### `PublishMetadata`

Optional metadata attached when publishing: a routing key and/or custom headers.
