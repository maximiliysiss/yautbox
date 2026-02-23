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
    /// Handle new messages and place them into the outbox
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
    /// Cancel outbox handling by ids
    /// WARNING: some messages can be already handled or be in process. They will be probably ignored
    /// </summary>
    /// <typeparam name="T">Payload type of the messages.</typeparam>
    /// <param name="ids">Outbox message identifiers to cancel.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task CancelAsync<T>(
        IEnumerable<OutboxMessageId> ids,
        CancellationToken cancellationToken = default);
}
