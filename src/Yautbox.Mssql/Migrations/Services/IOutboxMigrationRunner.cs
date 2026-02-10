using System.Threading;
using System.Threading.Tasks;

namespace Yautbox.Mssql.Migrations.Services;

internal interface IOutboxMigrationRunner
{
    Task MigrateUpAsync(CancellationToken cancellationToken);
}
