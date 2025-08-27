using Yautbox.Postgres.Environment;
using Yautbox.Postgres.Migrations.Services;

namespace Yautbox.Postgres.Extensions;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOutboxMigrations(this IServiceCollection services)
    {
        services
            .AddSingleton<IOutboxMigrationRunner, OutboxMigrationRunner>()
            .AddHostedService<MigrationService>();

        return services;
    }

    private sealed class MigrationService : BackgroundService
    {
        private readonly IOutboxMigrationRunner _runner;

        private readonly ISynchronizer _synchronizer;

        public MigrationService(IOutboxMigrationRunner runner, ISynchronizer synchronizer)
        {
            _runner = runner;
            _synchronizer = synchronizer;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await _runner.MigrateUpAsync(stoppingToken);

            await _synchronizer.ReadyAsync(stoppingToken);
        }
    }
}
