using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Yautbox.Entities;
using Yautbox.Runner.Options;

namespace Yautbox.Mysql.Repositories;

internal interface IMysqlOutboxRepository
{
    IAsyncEnumerable<OutboxMessage<T>> GetAsync<T>(
        string identifier,
        int count,
        TimeSpan locker,
        CancellationToken cancellationToken);

    IAsyncEnumerable<OutboxMessageId> AddAsync<T>(
        string identifier,
        IReadOnlyCollection<OutboxMessage<T>> messages,
        CancellationToken cancellationToken);

    Task DeleteAsync(
        IReadOnlyCollection<OutboxMessageId> ids,
        DeletePolicy policy,
        CancellationToken cancellationToken);

    Task UpdateAsync<T>(
        IReadOnlyCollection<OutboxMessage<T>> messages,
        CancellationToken cancellationToken);

    Task CleanAsync(
        string identifier,
        DateTimeOffset olderThan,
        CancellationToken cancellationToken);
}
