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
    private static readonly IAsyncDisposable _empty = new EmptyDisposable();
    private readonly ConcurrentDictionary<string, PostgresDistributedLock> _locks = [];

    public async Task<IAsyncDisposable> CreateAsync(
        string identifier,
        ExecutionPolicy policy,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        if (policy is ExecutionPolicy.Parallel)
            return _empty;

        var @lock = _locks.GetOrAdd(
            key: identifier,
            valueFactory: _ => new PostgresDistributedLock(
                key: new PostgresAdvisoryLockKey(identifier, allowHashing: true),
                connectionString: connectionFactory.GetConnectionString()));

        return await @lock.AcquireAsync(timeout: timeout, cancellationToken: cancellationToken);
    }

    private sealed class EmptyDisposable : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
