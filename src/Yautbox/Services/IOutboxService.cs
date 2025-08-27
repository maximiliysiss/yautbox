using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Yautbox.Services;

public interface IOutboxService
{
    Task AddAsync<TPayload>(IEnumerable<TPayload> messages, CancellationToken cancellationToken);
}
