using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Yautbox.Postgres.Infrastructure.Database;

/// <summary>
/// Provides database connections for the PostgreSQL outbox repository.
/// </summary>
public interface IOutboxConnectionFactory
{
    /// <summary>
    /// Gets the connection string used by the outbox.
    /// </summary>
    string GetConnectionString();

    /// <summary>
    /// Creates a database connection for outbox operations.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task<DbConnection> GetConnectionAsync(CancellationToken cancellationToken);
}
