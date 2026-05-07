using System.Data;
using ApplicationInfra.Books.Abstract;
using ApplicationInfra.Books.Oracle.Options;
using Microsoft.Extensions.Options;
using Oracle.ManagedDataAccess.Client;

namespace ApplicationInfra.Books.Oracle;

/// <summary>
/// Base class for a book loader that fetches data by running a SQL query against an Oracle database.
/// The connection string and query are configured through <see cref="OracleBookLoaderOptions"/>
/// under the <c>Books:{bookName}</c> configuration section.
/// Each row returned by the query is mapped to a single key/value entry in the book.
/// </summary>
/// <typeparam name="TKey">The book's key type.</typeparam>
/// <typeparam name="TValue">The book's value type.</typeparam>
/// <example>
/// <code>
/// // appsettings.json
/// // "Books": {
/// //   "Products": {
/// //     "ConnectionString": "Data Source=mydb;...",
/// //     "Query": "SELECT PRODUCT_ID, NAME, PRICE FROM PRODUCTS WHERE IS_ACTIVE = 1"
/// //   }
/// // }
///
/// public sealed class ProductBookLoader(IOptionsMonitor&lt;OracleBookLoaderOptions&gt; options)
///     : OracleBookLoader&lt;string, ProductConfig&gt;(options, "Products")
/// {
///     protected override (string Key, ProductConfig Value) MapRow(IDataRecord record) =&gt;
///         (record.GetString(0), new ProductConfig(record.GetString(1), record.GetDecimal(2)));
/// }
/// </code>
/// </example>
public abstract class OracleBookLoader<TKey, TValue> : IBookLoader<TKey, TValue>
    where TKey : notnull
{
    private readonly OracleBookLoaderOptions _options;

    protected OracleBookLoader(IOptionsMonitor<OracleBookLoaderOptions> optionsMonitor, string bookName)
    {
        _options = optionsMonitor.Get(bookName);
    }

    /// <summary>Maps a single result-set row to a key/value pair to store in the book.</summary>
    protected abstract (TKey Key, TValue Value) MapRow(IDataRecord record);

    /// <summary>
    /// Override to add bind parameters or change command settings before execution.
    /// <see cref="OracleCommand.CommandText"/> is already set to <see cref="OracleBookLoaderOptions.Query"/>.
    /// </summary>
    protected virtual void ConfigureCommand(OracleCommand command)
    {
    }

    public virtual async Task<IReadOnlyDictionary<TKey, TValue>> LoadAsync(CancellationToken cancellationToken)
    {
        await using var connection = new OracleConnection(_options.ConnectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        ConfigureCommand(command);
#pragma warning disable CA2100 // Query comes from trusted configuration, not user input.
        command.CommandText = _options.Query;
#pragma warning restore CA2100

        await using var reader = await command
            .ExecuteReaderAsync(cancellationToken)
            .ConfigureAwait(false);

        var data = new Dictionary<TKey, TValue>();
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var (key, value) = MapRow(reader);
            data[key] = value;
        }

        return data;
    }
}
