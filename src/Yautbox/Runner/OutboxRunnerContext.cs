using System;
using System.Collections.Generic;
using System.Linq;
using Yautbox.Entities;
using Yautbox.Infrastructure.DateTime;

namespace Yautbox.Runner;

internal sealed class OutboxRunnerContext<T>(IDateTimeProvider dateTimeProvider, IReadOnlyCollection<OutboxMessage<T>> outboxMessages)
{
    private readonly List<RetryRequest> _retries = [];
    public IReadOnlyCollection<RetryRequest> Retries => _retries.AsReadOnly();

    private readonly HashSet<OutboxMessageId> _success = outboxMessages.Select(c => c.Id).ToHashSet();
    public IEnumerable<OutboxMessageId> Success => _success;

    public bool IsSuccess => _success.Count > 0;

    public void AddRetry(Handlers.OutboxMessage<T> message, DateTimeOffset? at = null)
    {
        _success.Remove(message.Id);
        _retries.Add(new RetryRequest(message, at));
    }

    public void AddRetry(Handlers.OutboxMessage<T> message, TimeSpan? delay = null)
    {
        DateTimeOffset? at = null;
        if (delay.HasValue)
            at = dateTimeProvider.GetNow().Add(delay.Value);

        AddRetry(message, at);
    }

    public void Fail(TimeSpan delay)
    {
        var scheduledAt = dateTimeProvider.GetNow().Add(delay);

        var retries = _success
            .Join(outboxMessages, c => c, m => m.Id, (_, m) => m)
            .Select(m => new Handlers.OutboxMessage<T>(m, this))
            .ToArray();

        foreach (var retry in retries)
            AddRetry(retry, scheduledAt);
    }

    public sealed record RetryRequest(Handlers.OutboxMessage<T> Message, DateTimeOffset? ScheduledAt);
}
