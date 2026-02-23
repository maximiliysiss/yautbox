using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Yautbox.Mysql.Infrastructure.Database;

/// <summary>
/// Provides database connections for the MySQL outbox repository.
/// </summary>
public interface IOutboxConnectionFactory
{
    /// <summary>
    /// Gets the connection string used by the outbox.
    /// </summary>
    string GetConnectionString();

    /// <summary>
    /// Creates and opens a database connection.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task<DbConnection> GetConnectionAsync(CancellationToken cancellationToken);
}
