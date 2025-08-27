namespace Yautbox.Postgres.Migrations.Services;

public interface IOutboxMigrationRunner
{
    Task MigrateUpAsync(CancellationToken cancellationToken);
}
