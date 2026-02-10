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

public static class OutboxInfrastructureBuilderExtensions
{
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
