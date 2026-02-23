using System;
using System.Threading;
using System.Threading.Tasks;
using Yautbox.Runner.Options;

namespace Yautbox.Runner.Infrastructure;

/// <summary>
/// Creates execution policy scopes for outbox processing.
/// </summary>
public interface IPolicyFactory
{
    /// <summary>
    /// Creates a policy scope for the specified handler identifier.
    /// </summary>
    /// <param name="identifier">Outbox handler identifier.</param>
    /// <param name="policy">Execution policy to apply.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>An async disposable that ends the policy scope when disposed.</returns>
    Task<IAsyncDisposable> CreateAsync(string identifier, ExecutionPolicy policy, CancellationToken cancellationToken);
}
