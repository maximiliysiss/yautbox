using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Yautbox.Persistence;

public interface IOutboxConnectionFactory
{
    string GetConnectionString();
    Task<DbConnection> GetConnectionAsync(CancellationToken cancellationToken);
}
