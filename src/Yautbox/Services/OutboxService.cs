using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Yautbox.Persistence;

namespace Yautbox.Services;

internal class OutboxService : IOutboxService
{
    private readonly IOutboxRepository _outboxRepository;
    private readonly ILogger<OutboxService> _logger;

    public OutboxService(IOutboxRepository outboxRepository, ILogger<OutboxService> logger)
    {
        _outboxRepository = outboxRepository;
        _logger = logger;
    }

    public Task AddAsync<TPayload>(IEnumerable<TPayload> messages, CancellationToken cancellationToken)
    {
        _logger.AddedOutboxMessage();
        return _outboxRepository.AddAsync(messages, cancellationToken);
    }
}
