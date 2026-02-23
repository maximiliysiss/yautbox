using System.Threading;
using System.Threading.Tasks;

namespace Yautbox.Mysql.Migrations.Services;

internal interface IOutboxMigrationRunner
{
    Task MigrateUpAsync(CancellationToken cancellationToken);
}
