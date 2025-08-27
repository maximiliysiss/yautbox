using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Yautbox.Entities;

namespace Yautbox.Persistence;

public interface IOutboxRepository
{
    Task AddAsync<TPayload>(IEnumerable<TPayload> messages, CancellationToken cancellationToken);

    IAsyncEnumerable<OutboxMessage<TPayload>> ListAsync<TPayload>(int count, CancellationToken cancellationToken);

    Task DeleteAsync(IEnumerable<OutboxMessageId> messageIds, CancellationToken cancellationToken);
}
