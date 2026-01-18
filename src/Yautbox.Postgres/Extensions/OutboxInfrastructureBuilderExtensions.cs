using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Yautbox.Extensions.Builders.Outbox;
using Yautbox.Postgres.Environment;
using Yautbox.Postgres.Extensions.Configurator;
using Yautbox.Postgres.Migrations.Options;
using Yautbox.Postgres.Options;
using Yautbox.Postgres.Repositories;

namespace Yautbox.Postgres.Extensions;

public static class OutboxInfrastructureBuilderExtensions
{
    public static IOutboxInfrastructureBuilder UsePostgres<TConnectionFactory>(
        this IOutboxInfrastructureBuilder builder,
        PostgresStoreOptions? options = null)
    where TConnectionFactory : class, IOutboxConnectionFactory
    {
        var services = builder.Services;

        var syncAwaiter = new InfrastructureReadinessWaiter();
        builder
            .SetOutboxRepository<OutboxRepository>()
            .SetReadinessWaiter(syncAwaiter);

        options ??= new PostgresStoreOptions();

        services
            .AddOptions<PostgresOutboxRepositoryOptions>()
            .Configure<IOptions<OutboxSerializerOptions>>(
                (opt, jsonOptions) =>
                {
                    foreach (var converter in jsonOptions.Value.JsonSerializerOptions.Converters)
                        opt.JsonSerializerOptions.Converters.Add(converter);

                    if (options.SchemaName is not null)
                        opt.SchemaName = options.SchemaName;
                })
            .Services
            .AddOptions<MigrationOptions>()
            .Configure<IOptions<PostgresOutboxRepositoryOptions>>(
                (opt, repositoryOptions) => opt.SchemaName = repositoryOptions.Value.SchemaName);

        services
            .AddSingleton<IOutboxConnectionFactory, TConnectionFactory>()
            .AddSingleton(TimeProvider.System);

        services
            .AddSingleton<ISynchronizer>(syncAwaiter);

        services
            .AddOutboxMigrations();

        return builder;
    }
}
