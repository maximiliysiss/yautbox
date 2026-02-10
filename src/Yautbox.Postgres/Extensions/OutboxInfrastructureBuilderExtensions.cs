using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using Yautbox.Extensions.Builders.Outbox;
using Yautbox.Infrastructure.Waiter;
using Yautbox.Postgres.Environment;
using Yautbox.Postgres.Extensions.Configurator;
using Yautbox.Postgres.Infrastructure.Database;
using Yautbox.Postgres.Infrastructure.DateTime;
using Yautbox.Postgres.Options;
using Yautbox.Postgres.Policy;
using Yautbox.Postgres.Provider;
using Yautbox.Postgres.Repositories;

namespace Yautbox.Postgres.Extensions;

public static class OutboxInfrastructureBuilderExtensions
{
    public static IOutboxInfrastructureBuilder UsePostgres(
        this IOutboxInfrastructureBuilder builder,
        string connectionString,
        PostgresStoreOptions? options = null)
    {
        var services = builder.Services;

        services
            .TryAddSingleton<IOutboxConnectionFactory>(new DefaultConnectionFactory(connectionString));

        return builder.UsePostgres<DefaultConnectionFactory>(options);
    }

    public static IOutboxInfrastructureBuilder UsePostgres<TConnectionFactory>(
        this IOutboxInfrastructureBuilder builder,
        PostgresStoreOptions? options = null)
        where TConnectionFactory : class, IOutboxConnectionFactory
    {
        options ??= new PostgresStoreOptions();

        builder
            .SetProvider<PostgresOutboxProvider>()
            .SetWaiter<InfrastructureReadinessWaiter>()
            .SetPolicy<PostgresPolicyFactory>();

        var services = builder.Services;

        services
            .AddOptions<PostgresOutboxRepositoryOptions>()
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
            .TryAddScoped<IPostgresOutboxRepository, PostgresOutboxRepository>();

        return builder;

        void ConfigureSchemaName(PostgresOutboxRepositoryOptions opt, IServiceProvider provider)
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
            => Task.FromResult<DbConnection>(new NpgsqlConnection(connectionString));
    }
}
