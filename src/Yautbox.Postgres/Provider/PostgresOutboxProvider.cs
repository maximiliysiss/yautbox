using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Yautbox.Entities;
using Yautbox.Postgres.Repositories;
using Yautbox.Provider;
using Yautbox.Runner.Options;

namespace Yautbox.Postgres.Provider;

internal sealed class PostgresOutboxProvider(IPostgresOutboxRepository repository) : IOutboxProvider
{
    public async Task<IReadOnlyCollection<OutboxMessageId>> AddAsync<T>(
        string identifier,
        IReadOnlyCollection<OutboxMessage<T>> messages,
        CancellationToken cancellationToken)
    {
        return await repository
            .AddAsync(identifier, messages, cancellationToken)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<OutboxMessage<T>>> GetAsync<T>(
        string identifier,
        int count,
        TimeSpan visibility,
        CancellationToken cancellationToken)
    {
        return await repository
            .GetAsync<T>(identifier, count, visibility, cancellationToken)
            .ToArrayAsync(cancellationToken);
    }

    public Task CancelAsync(
        string identifier,
        IReadOnlyCollection<OutboxMessageId> ids,
        DeletePolicy policy,
        CancellationToken cancellationToken)
        => DeleteAsync(identifier, ids, policy, cancellationToken);

    public Task DeleteAsync(
        string identifier,
        IReadOnlyCollection<OutboxMessageId> ids,
        DeletePolicy policy,
        CancellationToken cancellationToken)
        => repository.DeleteAsync(ids, policy, cancellationToken);

    public Task CleanAsync(
        string identifier,
        DateTimeOffset olderThan,
        CancellationToken cancellationToken)
        => repository.CleanAsync(identifier, olderThan, cancellationToken);

    public Task RetryAsync<T>(
        string identifier,
        IReadOnlyCollection<OutboxMessage<T>> messages,
        CancellationToken cancellationToken)
        => repository.UpdateAsync(messages, cancellationToken);
}
