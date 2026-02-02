namespace Yautbox.Postgres.Migrations.Services;

internal interface IOutboxMigrationRunner
{
    Task MigrateUpAsync(CancellationToken cancellationToken);
}
