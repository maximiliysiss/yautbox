using System.Threading;
using System.Threading.Tasks;
using Yautbox.Entities;
using Yautbox.Services;

namespace Yautbox.Extensions.Outbox;

/// <summary>
/// Convenience helpers for the outbox service.
/// </summary>
public static class OutboxServiceExtensions
{
    /// <summary>
    /// Cancels a single outbox message by identifier.
    /// </summary>
    /// <typeparam name="T">Payload type of the messages.</typeparam>
    /// <param name="service">Outbox service instance.</param>
    /// <param name="id">Outbox message identifier.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    public static Task CancelAsync<T>(this IOutboxService service, OutboxMessageId id, CancellationToken cancellationToken = default)
        => service.CancelAsync<T>([id], cancellationToken);
}
