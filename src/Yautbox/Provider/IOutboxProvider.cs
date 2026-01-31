using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Yautbox.Entities;
using Yautbox.Runner.Options;

namespace Yautbox.Provider;

public interface IOutboxProvider
{
    /// <summary>
    /// Get outbox messages to handle
    /// </summary>
    Task<IReadOnlyCollection<OutboxMessage<T>>> GetAsync<T>(
        string identifier,
        int count,
        TimeSpan visibility,
        OutboxExecutionPolicy policy,
        CancellationToken cancellationToken);

    /// <summary>
    /// Add messages to outbox
    /// </summary>
    Task<IReadOnlyCollection<OutboxMessageId>> AddAsync<T>(
        string identifier,
        IReadOnlyCollection<OutboxMessage<T>> messages,
        CancellationToken cancellationToken);

    /// <summary>
    /// Cancel outbox handling by ids
    /// </summary>
    Task CancelAsync(
        IReadOnlyCollection<OutboxMessageId> ids,
        CancellationToken cancellationToken);

    /// <summary>
    /// Delete outbox messages by ids
    /// </summary>
    Task DeleteAsync(
        IReadOnlyCollection<OutboxMessageId> ids,
        OutboxDeletePolicy policy,
        CancellationToken cancellationToken);

    /// <summary>
    /// Clean safe-deleted outbox messages older than the specified date
    /// </summary>
    Task CleanAsync(
        DateTimeOffset olderThan,
        CancellationToken cancellationToken);

    /// <summary>
    /// Retry failed messages
    /// </summary>
    Task RetryAsync<T>(
        string identifier,
        IReadOnlyCollection<OutboxMessage<T>> messages,
        CancellationToken cancellationToken);
}
