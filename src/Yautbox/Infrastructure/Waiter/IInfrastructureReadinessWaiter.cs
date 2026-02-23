using System.Threading;
using System.Threading.Tasks;

namespace Yautbox.Infrastructure.Waiter;

/// <summary>
/// Waits for the outbox infrastructure to be ready.
/// </summary>
public interface IInfrastructureReadinessWaiter
{
    /// <summary>
    /// Waits until the infrastructure becomes ready.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the wait.</param>
    Task WaitAsync(CancellationToken cancellationToken);
}
