using System.Text.Json;
using Yautbox.Runner.Options;

namespace Yautbox.Postgres.Extensions.Configurator;

public sealed class PostgresStoreOptions
{
    /// <summary>
    /// Specifies the schema name used for managing the outbox database objects.
    /// If not explicitly set, defaults to the configured value in the system.
    /// Defaults to "outbox".
    /// </summary>
    public string? SchemaName { get; set; }

    /// <summary>
    /// An optional delegate used to configure <see cref="JsonSerializerOptions"/>
    /// for the Postgres outbox repository. This customization allows the user to
    /// modify serialization behavior during the lifecycle of the outbox data.
    /// The provided options and service provider can be utilized to apply
    /// application-specific configurations.
    /// </summary>
    public Action<JsonSerializerOptions, IServiceProvider>? ConfigureJsonOptions { get; set; }

    /// <summary>
    /// Defines the policy used for handling the deletion of processed outbox messages.
    /// The available options include "Safe" to preserve processed messages and "Delete" to remove them.
    /// Defaults to the "Delete" option if not explicitly configured.
    /// </summary>
    public DeletePolicy CancellationPolicy { get; set; } = DeletePolicy.Delete;

    /// <summary>
    /// Defines the batch size for cleaning up processed records in the outbox.
    /// This property determines the maximum number of records that will be removed
    /// in a single cleanup operation to manage database load and performance.
    /// Defaults to 1000.
    /// </summary>
    public int? CleanupBatchSize { get; set; }
}
