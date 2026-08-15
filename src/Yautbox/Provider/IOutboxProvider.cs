using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Yautbox.Entities;
using Yautbox.Provider.Contracts;
using Yautbox.Runner.Options;

namespace Yautbox.Provider;

/// <summary>
/// Defines storage operations for outbox messages.
/// </summary>
public interface IOutboxProvider
{
    /// <summary>
    /// Reads visible messages and reserves them for handling.
    /// </summary>
    /// <typeparam name="T">Payload type of the messages.</typeparam>
    /// <param name="identifier">Outbox handler identifier.</param>
    /// <param name="count">Maximum number of messages to return.</param>
    /// <param name="visibility">Visibility timeout for the messages.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task<IReadOnlyCollection<OutboxMessage<T>>> GetAsync<T>(
        string identifier,
        int count,
        TimeSpan visibility,
        CancellationToken cancellationToken);

    /// <summary>
    /// Persists messages in the outbox.
    /// </summary>
    /// <typeparam name="T">Payload type of the messages.</typeparam>
    /// <param name="messages">Messages to add to the outbox.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task<IReadOnlyCollection<OutboxMessageId>> AddAsync<T>(
        IReadOnlyCollection<AddRequest<T>> messages,
        CancellationToken cancellationToken);

    /// <summary>
    /// Cancels messages by identifier.
    /// </summary>
    /// <param name="identifier">Outbox handler identifier.</param>
    /// <param name="ids">Outbox message identifiers to cancel.</param>
    /// <param name="policy">Cancellation policy for the messages.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task CancelAsync(
        string identifier,
        IReadOnlyCollection<OutboxMessageId> ids,
        DeletePolicy policy,
        CancellationToken cancellationToken);

    /// <summary>
    /// Removes or marks messages as handled according to the delete policy.
    /// </summary>
    /// <param name="identifier">Outbox handler identifier.</param>
    /// <param name="ids">Outbox message identifiers to delete.</param>
    /// <param name="policy">Delete policy for the messages.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task DeleteAsync(
        string identifier,
        IReadOnlyCollection<OutboxMessageId> ids,
        DeletePolicy policy,
        CancellationToken cancellationToken);

    /// <summary>
    /// Permanently removes safe-deleted messages older than the specified timestamp.
    /// </summary>
    /// <param name="identifier">Outbox handler identifier.</param>
    /// <param name="olderThan">Remove messages older than this timestamp.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task CleanAsync(
        string identifier,
        DateTimeOffset olderThan,
        CancellationToken cancellationToken);

    /// <summary>
    /// Reschedules messages for retry.
    /// </summary>
    /// <typeparam name="T">Payload type of the messages.</typeparam>
    /// <param name="identifier">Outbox handler identifier.</param>
    /// <param name="messages">Messages to retry.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task RetryAsync<T>(
        string identifier,
        IReadOnlyCollection<OutboxMessage<T>> messages,
        CancellationToken cancellationToken);
}
