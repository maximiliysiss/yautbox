using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Yautbox.Entities;

namespace Yautbox.Services;

public interface IOutboxService
{
    /// <summary>
    /// Handle new messages and place them into the outbox
    /// </summary>
    Task<IEnumerable<OutboxMessageId>> HandleAsync<T>(
        IEnumerable<T> messages,
        DateTimeOffset? scheduledAt = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancel outbox handling by ids
    /// WARNING: some messages can be already handled or be in process. They will be probably ignored
    /// </summary>
    Task CancelAsync(IEnumerable<OutboxMessageId> ids, CancellationToken cancellationToken);
}
