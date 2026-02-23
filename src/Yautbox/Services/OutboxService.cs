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
using Yautbox.Metrics;
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

    private readonly IMetricsHandler _metricsHandler;

    public OutboxService(
        IOutboxProvider outboxProvider,
        ILogger<OutboxService> logger,
        IInfrastructureReadinessWaiter waiter,
        IDateTimeProvider dateTimeProvider,
        IOutboxRegistry registry,
        IMetricsHandler metricsHandler)
    {
        _outboxProvider = outboxProvider;
        _logger = logger;
        _waiter = waiter;
        _dateTimeProvider = dateTimeProvider;
        _registry = registry;
        _metricsHandler = metricsHandler;
    }

    public async Task<IEnumerable<OutboxMessageId>> HandleAsync<T>(
        IEnumerable<T> messages,
        DateTimeOffset? scheduledAt = null,
        CancellationToken cancellationToken = default)
    {
        await _waiter.WaitAsync(cancellationToken);

        _logger.AddedOutboxMessage();

        var identifier = _registry.GetIdentifier<T>();

        var outboxMessageIds = await _outboxProvider.AddAsync(
            identifier: identifier,
            messages: [.. messages.Select(Map)],
            cancellationToken: cancellationToken);

        await _metricsHandler.AddedAsync(
            identifier: identifier,
            count: outboxMessageIds.Count,
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

    public async Task CancelAsync<T>(
        IEnumerable<OutboxMessageId> ids,
        CancellationToken cancellationToken = default)
    {
        await _waiter.WaitAsync(cancellationToken);

        _logger.CancelOutboxMessage();

        var identifier = _registry.GetIdentifier<T>();
        var outboxMessageIds = ids.ToArray();

        await _outboxProvider.CancelAsync(
            identifier: identifier,
            ids: outboxMessageIds,
            policy: _registry.GetCancellationPolicy<T>(),
            cancellationToken: cancellationToken);

        await _metricsHandler.CanceledAsync(
            identifier: identifier,
            count: outboxMessageIds.Length,
            cancellationToken: cancellationToken);
    }
}
