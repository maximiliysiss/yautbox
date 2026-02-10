using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Yautbox.Mssql.Infrastructure.Database;

public interface IOutboxConnectionFactory
{
    string GetConnectionString();
    Task<DbConnection> GetConnectionAsync(CancellationToken cancellationToken);
}
