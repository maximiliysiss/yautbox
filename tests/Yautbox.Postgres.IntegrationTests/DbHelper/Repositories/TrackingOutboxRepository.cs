using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Yautbox.Entities;
using Yautbox.Postgres.IntegrationTests.DbHelper.Shared.Extensions;
using Yautbox.Postgres.Repositories;
using Yautbox.Provider.Contracts;
using Yautbox.Runner.Options;

namespace Yautbox.Postgres.IntegrationTests.DbHelper.Repositories;

internal sealed class TrackingOutboxRepository : IPostgresOutboxRepository
{
    private readonly IPostgresOutboxRepository _innerRepository;

    private readonly OutboxDbHelper _outboxDbHelper;

    public TrackingOutboxRepository(IPostgresOutboxRepository innerRepository, OutboxDbHelper outboxDbHelper)
    {
        _innerRepository = innerRepository;
        _outboxDbHelper = outboxDbHelper;
    }

    public IAsyncEnumerable<OutboxMessage<T>> GetAsync<T>(
        string identifier,
        int count,
        TimeSpan locker,
        CancellationToken cancellationToken)
        => _innerRepository.GetAsync<T>(identifier, count, locker, cancellationToken);

    public async IAsyncEnumerable<OutboxMessageId> AddAsync<T>(
        IReadOnlyCollection<AddRequest<T>> messages,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var ids = await _innerRepository
            .AddAsync(messages, cancellationToken)
            .ToArrayAsync(cancellationToken);

        _outboxDbHelper.Track(ids);

        foreach (var outboxMessageId in ids)
            yield return outboxMessageId;
    }

    public Task DeleteAsync(IReadOnlyCollection<OutboxMessageId> ids, DeletePolicy policy, CancellationToken cancellationToken)
        => _innerRepository.DeleteAsync(ids, policy, cancellationToken);

    public Task UpdateAsync<T>(IReadOnlyCollection<OutboxMessage<T>> messages, CancellationToken cancellationToken)
        => _innerRepository.UpdateAsync(messages, cancellationToken);

    public Task CleanAsync(string identifier, DateTimeOffset olderThan, CancellationToken cancellationToken)
        => _innerRepository.CleanAsync(identifier, olderThan, cancellationToken);
}
