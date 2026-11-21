using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Medallion.Threading.Postgres;
using Yautbox.Postgres.Infrastructure.Database;
using Yautbox.Runner.Infrastructure;
using Yautbox.Runner.Options;

namespace Yautbox.Postgres.Policy;

internal sealed class PostgresPolicyFactory(IOutboxConnectionFactory connectionFactory) : IPolicyFactory
{
    private readonly ConcurrentDictionary<string, PostgresDistributedLock> _locks = [];

    public async Task<IOutboxPolicyScope?> TryCreateAsync(
        string identifier,
        ExecutionPolicy policy,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        if (policy is ExecutionPolicy.Parallel)
            return EmptyScope.Instance;

        var @lock = _locks.GetOrAdd(
            key: identifier,
            valueFactory: _ => new PostgresDistributedLock(
                key: new PostgresAdvisoryLockKey(identifier, allowHashing: true),
                connectionString: connectionFactory.GetConnectionString()));

        var handle = await @lock.TryAcquireAsync(timeout: timeout, cancellationToken: cancellationToken);

        return handle is null ? null : new LockScope(handle);
    }

    private sealed class EmptyScope : IOutboxPolicyScope
    {
        public static readonly EmptyScope Instance = new();

        public CancellationToken HandleLostToken => CancellationToken.None;

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class LockScope(PostgresDistributedLockHandle handle) : IOutboxPolicyScope
    {
        public CancellationToken HandleLostToken => handle.HandleLostToken;

        public ValueTask DisposeAsync() => handle.DisposeAsync();
    }
}
