using System;
using System.Threading;

namespace Yautbox.Runner.Infrastructure;

/// <summary>
/// Represents an acquired execution policy scope for a single processing cycle.
/// </summary>
public interface IOutboxPolicyScope : IAsyncDisposable
{
    /// <summary>
    /// Gets a token that is canceled when the scope guarantee is lost (for example, when the connection holding
    /// a distributed lock is broken). Returns <see cref="CancellationToken.None"/> when loss cannot happen or
    /// cannot be detected.
    /// </summary>
    CancellationToken HandleLostToken { get; }
}
