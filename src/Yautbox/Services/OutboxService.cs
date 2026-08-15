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
using Yautbox.Provider.Contracts;
using Yautbox.Registy;
using Yautbox.Tracing;

namespace Yautbox.Services;

internal sealed class OutboxService : IOutboxService, IPolymorphicOutboxService
{
    private readonly IOutboxProvider _outboxProvider;

    private readonly IInfrastructureReadinessWaiter _waiter;

    private readonly IOutboxRegistry _registry;

    private readonly ILogger<OutboxService> _logger;

    private readonly IDateTimeProvider _dateTimeProvider;

    private readonly IMetricsHandler _metricsHandler;

    private readonly IOutboxTracer _tracer;

    public OutboxService(
        IOutboxProvider outboxProvider,
        ILogger<OutboxService> logger,
        IInfrastructureReadinessWaiter waiter,
        IDateTimeProvider dateTimeProvider,
        IOutboxRegistry registry,
        IMetricsHandler metricsHandler,
        IOutboxTracer tracer)
    {
        _outboxProvider = outboxProvider;
        _logger = logger;
        _waiter = waiter;
        _dateTimeProvider = dateTimeProvider;
        _registry = registry;
        _metricsHandler = metricsHandler;
        _tracer = tracer;
    }

    Task<IEnumerable<OutboxMessageId>> IOutboxService.HandleAsync<T>(
        IEnumerable<T> messages,
        DateTimeOffset? scheduledAt,
        CancellationToken cancellationToken)
    {
        return HandleAsync(
            messages: messages,
            scheduledAt: scheduledAt,
            type: typeof(T),
            cancellationToken: cancellationToken);
    }

    Task<IEnumerable<OutboxMessageId>> IPolymorphicOutboxService.HandleAsync<T>(
        IEnumerable<T> messages,
        DateTimeOffset? scheduledAt,
        CancellationToken cancellationToken)
    {
        return HandleAsync(
            messages: messages,
            scheduledAt: scheduledAt,
            type: null,
            cancellationToken: cancellationToken);
    }

    private async Task<IEnumerable<OutboxMessageId>> HandleAsync<T>(
        IEnumerable<T> messages,
        DateTimeOffset? scheduledAt,
        Type? type,
        CancellationToken cancellationToken)
    {
        await _waiter.WaitAsync(cancellationToken);

        var identifier = type is null ? null : _registry.GetIdentifier(type);

        var mappedMessages = messages
            .Select(Map)
            .ToArray();

        using var span = _tracer.StartEnqueue(
            identifier: identifier ?? _registry.GetIdentifier<T>(),
            count: mappedMessages.Length);

        try
        {
            var outboxMessageIds = await _outboxProvider.AddAsync(
                messages: mappedMessages,
                cancellationToken: cancellationToken);

            _logger.AddedOutboxMessage(
                identifier: identifier ?? _registry.GetIdentifier<T>(),
                count: mappedMessages.Length,
                scheduledAt: scheduledAt);

            await _metricsHandler.AddedAsync(
                identifier: identifier ?? _registry.GetIdentifier<T>(),
                count: outboxMessageIds.Count,
                cancellationToken: cancellationToken);

            return outboxMessageIds;
        }
        catch (Exception ex)
        {
            span.SetFailed(ex);
            throw;
        }

        AddRequest<T> Map(T payload)
        {
            ArgumentNullException.ThrowIfNull(payload);

            var message = new OutboxMessage<T>(
                Id: OutboxMessageId.Empty,
                Payload: payload,
                CreatedAt: _dateTimeProvider.GetNow(),
                Attempt: 0,
                ScheduledAt: scheduledAt);

            return new AddRequest<T>(
                Identifier: identifier ?? _registry.GetIdentifier(payload.GetType()),
                Message: message);
        }
    }

    public async Task CancelAsync<T>(
        IEnumerable<OutboxMessageId> ids,
        CancellationToken cancellationToken = default)
    {
        await _waiter.WaitAsync(cancellationToken);

        var identifier = _registry.GetIdentifier<T>();
        var outboxMessageIds = ids.ToArray();

        _logger.CancelOutboxMessage(identifier, outboxMessageIds.Length);

        using var span = _tracer.StartCancellation(identifier);

        try
        {
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
        catch (Exception ex)
        {
            span.SetFailed(ex);
            throw;
        }
    }
}
