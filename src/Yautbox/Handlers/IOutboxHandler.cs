using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Yautbox.Handlers;

public interface IOutboxHandler<TPayload>
{
    Task HandleAsync(IEnumerable<OutboxMessage<TPayload>> messages, CancellationToken cancellationToken);
}
