namespace ApplicationInfra.Books.Oracle.Options;

public sealed class OracleBookLoaderOptions
{
    /// <summary>Oracle connection string.</summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>The SQL SELECT query to execute on each refresh cycle.</summary>
    public string Query { get; set; } = string.Empty;
}
