using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Yautbox.Entities;
using Yautbox.Mysql.IntegrationTests.DbHelper.Shared.Extensions;
using Yautbox.Mysql.Repositories;
using Yautbox.Runner.Options;

namespace Yautbox.Mysql.IntegrationTests.DbHelper.Repositories;

internal sealed class TrackingOutboxRepository : IMysqlOutboxRepository
{
    private readonly IMysqlOutboxRepository _innerRepository;
    private readonly OutboxDbHelper _outboxDbHelper;

    public TrackingOutboxRepository(IMysqlOutboxRepository innerRepository, OutboxDbHelper outboxDbHelper)
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
        string identifier,
        IReadOnlyCollection<OutboxMessage<T>> messages,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var ids = await _innerRepository
            .AddAsync(identifier, messages, cancellationToken)
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
