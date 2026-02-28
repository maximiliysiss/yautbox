using System;
using System.Text.Json;

namespace Yautbox.Mysql.Extensions.Configurator;

/// <summary>
/// Configures the MySQL outbox repository.
/// </summary>
public sealed class MysqlStoreOptions
{
    /// <summary>
    /// Specifies the schema name used for managing the outbox database objects.
    /// If not explicitly set, defaults to the configured value in the system.
    /// Defaults to "outbox".
    /// </summary>
    public string? SchemaName { get; set; }

    /// <summary>
    /// An optional delegate used to configure <see cref="JsonSerializerOptions"/>
    /// for the MySQL outbox repository. This customization allows the user to
    /// modify serialization behavior during the lifecycle of the outbox data.
    /// The provided options and service provider can be utilized to apply
    /// application-specific configurations.
    /// </summary>
    public Action<JsonSerializerOptions, IServiceProvider>? ConfigureJsonOptions { get; set; }

    /// <summary>
    /// Defines the batch size for cleaning up processed records in the outbox.
    /// This property determines the maximum number of records that will be removed
    /// in a single cleanup operation to manage database load and performance.
    /// Defaults to 1000.
    /// </summary>
    public int? CleanupBatchSize { get; set; }

    /// <summary>
    /// Defines the number of retry attempts to perform in the event of a database deadlock
    /// during operations on the outbox. If not specified, no retries are attempted.
    /// Defaults to 3.
    /// </summary>
    public int? DeadlockRetryCount { get; set; }

    /// <summary>
    /// Defines the duration that the system should wait before retrying
    /// an operation after encountering a deadlock.
    /// This property is optional and, if not set, defaults to the
    /// system's preconfigured behavior for handling deadlock retries.
    /// Defaults to 50 milliseconds.
    /// </summary>
    public TimeSpan? DeadlockDelay { get; set; }
}
