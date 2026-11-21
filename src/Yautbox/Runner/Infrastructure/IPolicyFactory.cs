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
    /// Tries to create a policy scope for the specified handler identifier.
    /// </summary>
    /// <param name="identifier">Outbox handler identifier.</param>
    /// <param name="policy">Execution policy to apply.</param>
    /// <param name="timeout">Maximum time to wait for the policy scope to become available.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>
    /// A scope that ends when disposed, or <see langword="null"/> when the scope is held by another worker
    /// and could not be acquired within <paramref name="timeout"/>.
    /// </returns>
    Task<IOutboxPolicyScope?> TryCreateAsync(string identifier, ExecutionPolicy policy, TimeSpan timeout, CancellationToken cancellationToken);
}
