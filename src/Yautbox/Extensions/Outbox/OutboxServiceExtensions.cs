using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Yautbox.Entities;
using Yautbox.Services;

namespace Yautbox.Extensions.Outbox;

public static class OutboxServiceExtensions
{
    public static async Task<OutboxMessageId> HandleAsync<TPayload>(
        this IOutboxService service,
        TPayload message,
        DateTimeOffset? scheduledAt = null,
        CancellationToken cancellationToken = default)
    {
        var outboxMessageIds = await service.HandleAsync(messages: [message], scheduledAt, cancellationToken);
        return outboxMessageIds.Single();
    }

    public static Task CancelAsync(this IOutboxService service, OutboxMessageId id, CancellationToken cancellationToken)
        => service.CancelAsync([id], cancellationToken);
}
