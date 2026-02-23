using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Yautbox.Extensions.Builders.Outbox;
using Yautbox.Infrastructure.Waiter;
using Yautbox.Mssql.Environment;
using Yautbox.Mssql.Extensions.Configurator;
using Yautbox.Mssql.Infrastructure.Database;
using Yautbox.Mssql.Infrastructure.DateTime;
using Yautbox.Mssql.Options;
using Yautbox.Mssql.Policy;
using Yautbox.Mssql.Provider;
using Yautbox.Mssql.Repositories;

namespace Yautbox.Mssql.Extensions;

/// <summary>
/// Extension methods for configuring the MSSQL outbox infrastructure.
/// </summary>
public static class OutboxInfrastructureBuilderExtensions
{
    /// <summary>
    /// Configures MSSQL outbox infrastructure using a connection string.
    /// </summary>
    /// <param name="builder">Infrastructure builder to configure.</param>
    /// <param name="connectionString">Connection string for the MSSQL database.</param>
    /// <param name="options">Optional store configuration overrides.</param>
    /// <returns>The same builder instance for chaining.</returns>
    public static IOutboxInfrastructureBuilder UseMssql(
        this IOutboxInfrastructureBuilder builder,
        string connectionString,
        MssqlStoreOptions? options = null)
    {
        var services = builder.Services;

        services
            .TryAddSingleton<IOutboxConnectionFactory>(new DefaultConnectionFactory(connectionString));

        return builder.UseMssql<DefaultConnectionFactory>(options);
    }

    /// <summary>
    /// Configures MSSQL outbox infrastructure using a connection factory.
    /// </summary>
    /// <typeparam name="TConnectionFactory">Connection factory type.</typeparam>
    /// <param name="builder">Infrastructure builder to configure.</param>
    /// <param name="options">Optional store configuration overrides.</param>
    /// <returns>The same builder instance for chaining.</returns>
    public static IOutboxInfrastructureBuilder UseMssql<TConnectionFactory>(
        this IOutboxInfrastructureBuilder builder,
        MssqlStoreOptions? options = null)
        where TConnectionFactory : class, IOutboxConnectionFactory
    {
        options ??= new MssqlStoreOptions();

        builder
            .SetProvider<MssqlOutboxProvider>()
            .SetWaiter<InfrastructureReadinessWaiter>()
            .SetPolicy<MssqlPolicyFactory>();

        var services = builder.Services;

        services
            .AddOptions<MssqlOutboxRepositoryOptions>()
            .Configure<IServiceProvider>(ConfigureSchemaName);

        services
            .TryAddSingleton<IOutboxConnectionFactory, TConnectionFactory>();

        services
            .TryAddSingleton<IDateTimeProvider, DateTimeProvider>();

        services
            .TryAddSingleton<ISynchronizer>(sp => (ISynchronizer)sp.GetRequiredService<IInfrastructureReadinessWaiter>());

        services
            .AddOutboxMigrations();

        services
            .TryAddScoped<IMssqlOutboxRepository, MssqlOutboxRepository>();

        return builder;

        void ConfigureSchemaName(MssqlOutboxRepositoryOptions opt, IServiceProvider provider)
        {
            if (!string.IsNullOrWhiteSpace(options.SchemaName))
                opt.SchemaName = options.SchemaName;

            if (options.CleanupBatchSize.HasValue)
                opt.CleanupBatchSize = options.CleanupBatchSize.Value;

            options.ConfigureJsonOptions?.Invoke(opt.JsonSerializerOptions, provider);
        }
    }

    private sealed class DefaultConnectionFactory(string connectionString) : IOutboxConnectionFactory
    {
        public string GetConnectionString() => connectionString;

        public Task<DbConnection> GetConnectionAsync(CancellationToken cancellationToken)
            => Task.FromResult<DbConnection>(new SqlConnection(connectionString));
    }
}
