using System.Threading;
using System.Threading.Tasks;
using Yautbox.Entities;
using Yautbox.Services;

namespace Yautbox.Extensions.Outbox;

public static class OutboxServiceExtensions
{
    public static Task CancelAsync<T>(this IOutboxService service, OutboxMessageId id, CancellationToken cancellationToken = default)
        => service.CancelAsync<T>([id], cancellationToken);
}
