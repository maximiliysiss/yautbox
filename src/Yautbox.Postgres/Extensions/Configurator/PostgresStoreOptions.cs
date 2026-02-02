using System.Text.Json;
using Yautbox.Runner.Options;

namespace Yautbox.Postgres.Extensions.Configurator;

public sealed class PostgresStoreOptions
{
    /// <summary>
    /// Specifies the schema name used for managing the outbox database objects.
    /// If not explicitly set, defaults to the configured value in the system.
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
    public OutboxDeletePolicy CancellationPolicy { get; set; } = OutboxDeletePolicy.Delete;
}
