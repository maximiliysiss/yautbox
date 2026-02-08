using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Yautbox.Postgres.Environment;
using Yautbox.Postgres.Migrations.Options;
using Yautbox.Postgres.Migrations.Services;
using Yautbox.Postgres.Options;

namespace Yautbox.Postgres.Extensions;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOutboxMigrations(this IServiceCollection services)
    {
        services
            .AddOptions<MigrationOptions>()
            .Configure<IOptions<PostgresOutboxRepositoryOptions>>((opt, repositoryOptions) =>
                opt.SchemaName = repositoryOptions.Value.SchemaName);

        services
            .TryAddSingleton<IOutboxMigrationRunner, OutboxMigrationRunner>();

        services
            .AddHostedService<MigrationService>();

        return services;
    }

    private sealed class MigrationService(IOutboxMigrationRunner runner, ISynchronizer synchronizer) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await runner.MigrateUpAsync(stoppingToken);
            await synchronizer.ReadyAsync(stoppingToken);
        }
    }
}
