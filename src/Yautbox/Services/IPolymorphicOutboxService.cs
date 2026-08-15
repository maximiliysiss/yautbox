using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Yautbox.Entities;

namespace Yautbox.Services;

/// <summary>
/// Provides an API for enqueuing a heterogeneous batch under each payload's concrete runtime type.
/// </summary>
public interface IPolymorphicOutboxService
{
    /// <summary>
    /// Enqueues messages for later processing, routing every message to the handler registered for its runtime type.
    /// </summary>
    /// <typeparam name="T">Common compile-time type of the messages.</typeparam>
    /// <param name="messages">Messages to enqueue. Null payloads are not supported.</param>
    /// <param name="scheduledAt">Optional time when the messages become visible.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>Identifiers of the created messages in the same order as <paramref name="messages"/>.</returns>
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
