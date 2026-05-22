using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Yautbox.Entities;

namespace Yautbox.Services;

/// <summary>
/// Provides APIs for enqueuing and canceling outbox messages.
/// </summary>
public interface IOutboxService
{
    /// <summary>
    /// Enqueues messages for later processing by the registered handler for <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">Payload type of the messages.</typeparam>
    /// <param name="messages">Messages to enqueue.</param>
    /// <param name="scheduledAt">Optional time when the messages become visible.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>Identifiers of the created outbox messages.</returns>
    Task<IEnumerable<OutboxMessageId>> HandleAsync<T>(
        IEnumerable<T> messages,
        DateTimeOffset? scheduledAt = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels queued messages by identifier.
    /// </summary>
    /// <remarks>Messages that were already handled or are currently being handled may not be canceled.</remarks>
    /// <typeparam name="T">Payload type of the messages.</typeparam>
    /// <param name="ids">Outbox message identifiers to cancel.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task CancelAsync<T>(
        IEnumerable<OutboxMessageId> ids,
        CancellationToken cancellationToken = default);
}
