using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MySqlConnector;
using Yautbox.Extensions.Builders.Outbox;
using Yautbox.Infrastructure.Waiter;
using Yautbox.Mysql.Environment;
using Yautbox.Mysql.Extensions.Configurator;
using Yautbox.Mysql.Infrastructure.Database;
using Yautbox.Mysql.Infrastructure.DateTime;
using Yautbox.Mysql.Options;
using Yautbox.Mysql.Policy;
using Yautbox.Mysql.Provider;
using Yautbox.Mysql.Repositories;

namespace Yautbox.Mysql.Extensions;

/// <summary>
/// Extension methods for configuring the MySQL outbox infrastructure.
/// </summary>
public static class OutboxInfrastructureBuilderExtensions
{
    /// <summary>
    /// Configures MySQL outbox infrastructure using a connection string.
    /// </summary>
    /// <param name="builder">Infrastructure builder to configure.</param>
    /// <param name="connectionString">Connection string for the MySQL database.</param>
    /// <param name="options">Optional store configuration overrides.</param>
    /// <returns>The same builder instance for chaining.</returns>
    public static IOutboxInfrastructureBuilder UseMysql(
        this IOutboxInfrastructureBuilder builder,
        string connectionString,
        MysqlStoreOptions? options = null)
    {
        var services = builder.Services;

        services
            .TryAddSingleton<IOutboxConnectionFactory>(new DefaultConnectionFactory(connectionString));

        return builder.UseMysql<DefaultConnectionFactory>(options);
    }

    /// <summary>
    /// Configures MySQL outbox infrastructure using a connection factory.
    /// </summary>
    /// <typeparam name="TConnectionFactory">Connection factory type.</typeparam>
    /// <param name="builder">Infrastructure builder to configure.</param>
    /// <param name="options">Optional store configuration overrides.</param>
    /// <returns>The same builder instance for chaining.</returns>
    public static IOutboxInfrastructureBuilder UseMysql<TConnectionFactory>(
        this IOutboxInfrastructureBuilder builder,
        MysqlStoreOptions? options = null)
        where TConnectionFactory : class, IOutboxConnectionFactory
    {
        options ??= new MysqlStoreOptions();

        builder
            .SetProvider<MysqlOutboxProvider>()
            .SetWaiter<InfrastructureReadinessWaiter>()
            .SetPolicy<MysqlPolicyFactory>();

        var services = builder.Services;

        services
            .AddOptions<MysqlOutboxRepositoryOptions>()
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
            .TryAddScoped<IMysqlOutboxRepository, MysqlOutboxRepository>();

        return builder;

        void ConfigureSchemaName(MysqlOutboxRepositoryOptions opt, IServiceProvider provider)
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
            => Task.FromResult<DbConnection>(new MySqlConnection(connectionString));
    }
}
