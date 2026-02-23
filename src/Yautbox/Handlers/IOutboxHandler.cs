using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Yautbox.Handlers;

/// <summary>
/// Handles outbox messages for a specific payload type.
/// </summary>
/// <typeparam name="TPayload">Payload type handled by this handler.</typeparam>
public interface IOutboxHandler<TPayload>
{
    /// <summary>
    /// Processes a batch of outbox messages.
    /// </summary>
    /// <param name="messages">Messages to process.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task HandleAsync(IEnumerable<OutboxMessage<TPayload>> messages, CancellationToken cancellationToken);
}
