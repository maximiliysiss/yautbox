using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Yautbox.Entities;
using Yautbox.Runner.Options;

namespace Yautbox.Provider;

public interface IOutboxProvider
{
    Task<IReadOnlyCollection<OutboxMessage<T>>> GetAsync<T>(
        int count,
        TimeSpan visibility,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<OutboxMessageId>> AddAsync<T>(
        IReadOnlyCollection<OutboxMessage<T>> messages,
        CancellationToken cancellationToken);

    Task CancelAsync(
        IReadOnlyCollection<OutboxMessageId> ids,
        CancellationToken cancellationToken);

    Task DeleteAsync(
        IReadOnlyCollection<OutboxMessageId> ids,
        OutboxDeletePolicy policy,
        CancellationToken cancellationToken);

    Task RetryAsync<T>(
        IReadOnlyCollection<OutboxMessage<T>> messages,
        CancellationToken cancellationToken);
}
