using System;
using System.Text.Json;

namespace Yautbox.Postgres.Extensions.Configurator;

/// <summary>
/// Configures PostgreSQL storage for the outbox provider.
/// </summary>
public sealed class PostgresStoreOptions
{
    /// <summary>
    /// Gets or sets the schema that contains the outbox tables. The default is <c>outbox</c>.
    /// </summary>
    public string? SchemaName { get; set; }

    /// <summary>
    /// Gets or sets a delegate that configures <see cref="JsonSerializerOptions"/> used to serialize payloads.
    /// </summary>
    public Action<JsonSerializerOptions, IServiceProvider>? ConfigureJsonOptions { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of safe-deleted rows removed in one cleanup operation. The default is 1000.
    /// </summary>
    public int? CleanupBatchSize { get; set; }
}
