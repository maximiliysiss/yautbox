using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Yautbox.Handlers;

public interface IOutboxHandler<in TPayload>
{
    Task HandleAsync(IEnumerable<TPayload> payloads, CancellationToken cancellationToken);
}
