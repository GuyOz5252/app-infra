namespace ApplicationInfra.Serialization.SchemaRegistry.Options;

public sealed class SchemaRegistryOptions
{
    public string Url { get; set; } = string.Empty;
    public string? BasicAuthUserInfo { get; set; }
}
