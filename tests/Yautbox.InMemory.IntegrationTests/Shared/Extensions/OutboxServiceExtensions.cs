using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Yautbox.Entities;
using Yautbox.Services;

namespace Yautbox.InMemory.IntegrationTests.Shared.Extensions;

internal static class OutboxServiceExtensions
{
    public static async Task<OutboxMessageId> HandleAsync<T>(
        this IOutboxService service,
        T message,
        DateTimeOffset? scheduledAt = null,
        CancellationToken cancellationToken = default)
    {
        var outboxMessageIds = await service.HandleAsync(messages: [message], scheduledAt, cancellationToken);
        return outboxMessageIds.Single();
    }
}
