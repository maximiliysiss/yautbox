using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Yautbox.Entities;
using Yautbox.Extensions.Logger;
using Yautbox.Infrastructure.DateTime;
using Yautbox.Infrastructure.Waiter;
using Yautbox.Provider;
using Yautbox.Registy;

namespace Yautbox.Services;

internal sealed class OutboxService : IOutboxService
{
    private readonly IOutboxProvider _outboxProvider;

    private readonly IInfrastructureReadinessWaiter _waiter;

    private readonly IOutboxRegistry _registry;

    private readonly ILogger<OutboxService> _logger;

    private readonly IDateTimeProvider _dateTimeProvider;

    public OutboxService(
        IOutboxProvider outboxProvider,
        ILogger<OutboxService> logger,
        IInfrastructureReadinessWaiter waiter,
        IDateTimeProvider dateTimeProvider,
        IOutboxRegistry registry)
    {
        _outboxProvider = outboxProvider;
        _logger = logger;
        _waiter = waiter;
        _dateTimeProvider = dateTimeProvider;
        _registry = registry;
    }

    public async Task<IEnumerable<OutboxMessageId>> HandleAsync<T>(
        IEnumerable<T> messages,
        DateTimeOffset? scheduledAt = null,
        CancellationToken cancellationToken = default)
    {
        await _waiter.WaitAsync(cancellationToken);

        _logger.AddedOutboxMessage();

        var outboxMessageIds = await _outboxProvider.AddAsync(
            identifier: _registry.GetIdentifier<T>(),
            messages: [.. messages.Select(Map)],
            cancellationToken: cancellationToken);

        return outboxMessageIds;

        OutboxMessage<T> Map(T payload)
        {
            return new OutboxMessage<T>(
                Id: OutboxMessageId.Empty,
                Payload: payload,
                CreatedAt: _dateTimeProvider.GetNow(),
                Attempt: 0,
                ScheduledAt: scheduledAt);
        }
    }

    public async Task CancelAsync(
        IEnumerable<OutboxMessageId> ids,
        CancellationToken cancellationToken)
    {
        await _waiter.WaitAsync(cancellationToken);

        _logger.CancelOutboxMessage();

        await _outboxProvider.CancelAsync([.. ids], cancellationToken);
    }
}
