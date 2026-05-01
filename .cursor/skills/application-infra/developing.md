# ApplicationInfra — Developer Reference

## Solution & Build

- **Solution:** `ApplicationInfra.slnx` (new format; open with VS 2022 17.10+ or `dotnet` CLI)
- **Shared MSBuild defaults:** `Directory.Build.props` (applies to all projects automatically)
- **Central package management:** `Directory.Packages.props` (CPM enabled — set versions here, not in individual `.csproj` files)

```bash
dotnet build
dotnet build -c Release
```

### Packable projects

All projects inherit `GeneratePackageOnBuild = true` from `Directory.Build.props`.
`ApplicationInfra.Sample` overrides this to `false` — it is a demo app, not a package.

`ApplicationInfra.Messaging.Kafka.MassTransit` is currently an empty placeholder. Set `<IsPackable>false</IsPackable>` in its `.csproj` or remove it from packing until it has a real implementation.

### PackageVersion

**Do not** set `PackageVersion` in source. CI injects it at pack time:

```bash
dotnet pack -c Release --no-build -p:PackageVersion=1.2.3
```

For local testing, supply any version:

```bash
dotnet pack -c Release -p:PackageVersion=0.0.1-local
```

---

## Adding a New Package

1. Add a new class library project under the root (same level as existing packages).
2. Reference it in `ApplicationInfra.slnx`.
3. Set `<PackageId>` in the `.csproj` only if the assembly name differs from the desired package name — otherwise it defaults to the project name.
4. Do **not** copy `Authors`, `Description`, `RepositoryUrl` etc. — they are inherited from `Directory.Build.props`.
5. Add any new third-party packages to `Directory.Packages.props` (version only) and reference them from the `.csproj` without a version.
6. Add a project reference to `ApplicationInfra.Sample` if the new package needs exercising.

---

## Code Style Rules

These are enforced at build time (`EnforceCodeStyleInBuild = true`, `TreatWarningsAsErrors = true`).

### Braces — always required

```csharp
// ❌
if (x == null)
    return;

// ✅
if (x == null)
{
    return;
}
```

Exception: expression-bodied **property getters** are fine (`public string Name => _name;`).

### No arrow bodies on class methods

```csharp
// ❌
public Task StartAsync(CancellationToken ct) => Task.CompletedTask;

// ✅
public Task StartAsync(CancellationToken ct)
{
    return Task.CompletedTask;
}
```

### ILogger first in constructors and fields

```csharp
// ✅
private readonly ILogger<MyService> _logger;
private readonly IMyDependency _dep;

public MyService(ILogger<MyService> logger, IMyDependency dep)
{
    _logger = logger;
    _dep = dep;
}
```

### Private fields: `_camelCase`, `readonly` when possible

```csharp
// ✅
private readonly IEventSerializer _serializer;
private CancellationTokenSource? _cts;
```

### Minimal member visibility

Use the least-visible modifier that works:
- Classes that are internal to the package: `internal sealed`
- Event processors in consuming services: `internal sealed`
- Public API surface: `public` only for types that consumers need to reference

### One type per file

One top-level `class`, `record`, `interface`, or `struct` per `.cs` file, named to match.

**Allowed exceptions:**
- Request/response records co-located in the same file as the endpoint that uses them
- Handler + its tightly-coupled command/result types

### Options validation: not inside services

Do not guard options values inside the service that consumes them. Use `IValidateOptions<T>` (with `ValidateOnStart`) or data annotations on the options type.

```csharp
// ❌ — guard in service
public async Task ProduceAsync<TEvent>(TEvent @event, CancellationToken ct)
{
    var opts = _optionsMonitor.Get(_name);
    if (string.IsNullOrWhiteSpace(opts.Topic)) throw new InvalidOperationException("...");
    ...
}

// ✅ — trust validated options
public async Task ProduceAsync<TEvent>(TEvent @event, CancellationToken ct)
{
    var opts = _optionsMonitor.Get(_name);
    await _producer.ProduceAsync(opts.Topic, ...).ConfigureAwait(false);
}
```

### Folder naming: plural, except `Abstract`

```
✅ Extensions/
✅ Options/
✅ Abstract/      ← shared interfaces/abstractions only
❌ Extension/
```

### Logging

Use `LoggerMessage` source generation for all log calls inside library code (not just sample code):

```csharp
internal static partial class MyLogger
{
    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to consume: {Reason}")]
    internal static partial void ConsumeError(this ILogger logger, string reason);
}
```

---

## DI Patterns

### Keyed services

Kafka producers and consumers use named/keyed registrations. The `name` string is the config section key and the DI service key:

```csharp
services.AddKeyedSingleton<IEventPublisher>(name, (sp, _) => new KafkaProducer<TSerializer>(...));
services.AddKeyedScoped<TEventProcessor>(name);
```

### Extension blocks (C# 14 preview)

`ServiceCollectionExtensions` in the Kafka package uses the new C# **extension blocks** syntax:

```csharp
public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public void AddKafkaProducer<TSerializer>(...) { ... }
    }
}
```

This requires a C# 14+ toolchain. Ensure the consuming SDK supports it before adding similar patterns to other packages.

### `TryAdd*` vs `Add*`

- Use `TryAddSingleton` / `TryAddEnumerable` for library registrations to avoid accidental duplicate registration.
- `AddHostedService` is acceptable for consumers (each consumer is intentionally a separate hosted service).

---

## Publishing (CI)

**Workflow:** `.github/workflows/publish.yml`

| Trigger | Version format |
|---------|---------------|
| `release` (type: `published`) | Tag name (e.g. `1.2.3`) |
| `workflow_dispatch` | `{latest-tag}-dev.{run_number}` |

**Steps:** checkout → restore → `dotnet build -c Release` → `dotnet test -c Release --no-build` → `dotnet pack -c Release --no-build -p:PackageVersion=...` → upload artifact → push to GitHub Packages.

> `dotnet test` currently succeeds vacuously because there are no test projects. If test projects are added they will be picked up automatically.

### Publishing to GitHub Packages locally

```bash
dotnet nuget push "**/*.nupkg" \
  --source "https://nuget.pkg.github.com/GuyOz5252/index.json" \
  --api-key YOUR_PAT \
  --skip-duplicate
```

---

## Project Dependency Graph

```
ApplicationInfra.Api
  (no internal deps)

ApplicationInfra.Serialization
  (no internal deps)

ApplicationInfra.Messaging.Abstractions
  (no internal deps)

ApplicationInfra.Messaging.Kafka
  → ApplicationInfra.Messaging.Abstractions
  → ApplicationInfra.Serialization

ApplicationInfra.Messaging.Kafka.MassTransit  ← PLACEHOLDER, empty
  → ApplicationInfra.Messaging.Abstractions
  → ApplicationInfra.Serialization
```

---

## Suppressions

The following warnings are suppressed globally in `Directory.Build.props`:

| Code | Reason |
|------|--------|
| `NU1507` | CPM with multiple feeds — expected |
| `CA2016` | CancellationToken forwarding — intentional in some Confluent call sites |
| `S1075` | Hardcoded URI strings — intentional in DI extensions |
| `CA1873` | Unnecessary `LoggerMessage` pattern suggestion — project uses source gen |
