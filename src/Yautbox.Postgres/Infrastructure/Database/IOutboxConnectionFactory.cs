using System.Data.Common;

namespace Yautbox.Postgres.Infrastructure.Database;

public interface IOutboxConnectionFactory
{
    string GetConnectionString();
    Task<DbConnection> GetConnectionAsync(CancellationToken cancellationToken);
}
