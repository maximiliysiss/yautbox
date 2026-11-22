using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Yautbox.Runner.Infrastructure;
using Yautbox.Runner.Options;

namespace Yautbox.InMemory.Policy;

/// <summary>
/// Enforces <see cref="ExecutionPolicy.Sequential"/> with an in-process lock per identifier, so the workers of one
/// process never process a cycle concurrently.
/// </summary>
/// <remarks>Registered as a singleton: the locks must be shared by all workers of the process.</remarks>
internal sealed class InMemoryPolicyFactory : IPolicyFactory
{
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = [];

    public async Task<IOutboxPolicyScope?> TryCreateAsync(
        string identifier,
        ExecutionPolicy policy,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        if (policy is ExecutionPolicy.Parallel)
            return EmptyScope.Instance;

        var semaphore = _locks.GetOrAdd(identifier, _ => new SemaphoreSlim(1, 1));

        if (!await semaphore.WaitAsync(timeout, cancellationToken))
            return null;

        return new LockScope(semaphore);
    }

    private sealed class EmptyScope : IOutboxPolicyScope
    {
        public static readonly EmptyScope Instance = new();

        public CancellationToken HandleLostToken => CancellationToken.None;

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class LockScope(SemaphoreSlim semaphore) : IOutboxPolicyScope
    {
        private int _released;

        // An in-process lock cannot be lost while the scope is held.
        public CancellationToken HandleLostToken => CancellationToken.None;

        public ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _released, 1) is 0)
                semaphore.Release();

            return ValueTask.CompletedTask;
        }
    }
}
