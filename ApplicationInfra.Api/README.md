# ApplicationInfra.Api

Convention-based endpoint registration for ASP.NET Core minimal APIs. Implements the `IEndpoint` pattern so route definitions are kept in dedicated classes rather than `Program.cs`.

## `IEndpoint`

Implement this interface once per logical endpoint group:

```csharp
public class OrderEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/orders", HandleAsync);
    }

    private static async Task<IResult> HandleAsync(...)
    {
        // ...
    }
}
```

## Registration

```csharp
// Scans the given assembly and registers all IEndpoint implementations
builder.Services.AddEndpoints(typeof(Program).Assembly);
```

## Mapping

```csharp
// Maps all registered endpoints on the WebApplication
app.MapEndpoints();

// Optionally scope them under a route group
var v1 = app.MapGroup("/v1");
app.MapEndpoints(v1);
```
