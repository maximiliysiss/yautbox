using System;
using System.Collections.Generic;
using System.Linq;
using Yautbox.Entities;
using Yautbox.Infrastructure.DateTime;
using Yautbox.Runner.Options;

namespace Yautbox.Runner.Infrastructure;

internal sealed class OutboxRunnerContext<T>
{
    private readonly List<RetryRequest> _retries = [];
    public IReadOnlyCollection<RetryRequest> Retries => _retries.AsReadOnly();

    private readonly HashSet<OutboxMessageId> _success;
    public IEnumerable<OutboxMessageId> Success => _success;

    private readonly List<OutboxMessageId> _retryExceeded = [];
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IReadOnlyCollection<OutboxMessage<T>> _outboxMessages;
    private readonly IOutboxRunnerOptions _options;

    public OutboxRunnerContext(
        IDateTimeProvider dateTimeProvider,
        IReadOnlyCollection<OutboxMessage<T>> outboxMessages,
        IOutboxRunnerOptions options)
    {
        _dateTimeProvider = dateTimeProvider;
        _outboxMessages = outboxMessages;
        _options = options;
        _success = outboxMessages.Select(c => c.Id).ToHashSet();
    }

    public IReadOnlyCollection<OutboxMessageId> RetryExceeded => _retryExceeded.AsReadOnly();

    public bool IsFailed { get; private set; }

    public bool IsSuccess => _success.Count > 0;

    public void AddRetry(Handlers.OutboxMessage<T> message, DateTimeOffset? at = null)
    {
        if (_options.RetryCount is { } retryCount && message.Attempt + 1 > retryCount)
        {
            _success.Remove(message.Id);
            _retryExceeded.Add(message.Id);
            return;
        }

        _success.Remove(message.Id);
        _retries.Add(new RetryRequest(message, at));
    }

    public void AddRetry(Handlers.OutboxMessage<T> message, TimeSpan? delay = null)
    {
        DateTimeOffset? at = null;
        if (delay.HasValue)
            at = _dateTimeProvider.GetNow().Add(delay.Value);

        AddRetry(message, at);
    }

    public void Fail()
    {
        IsFailed = true;

        var retries = _success
            .Join(_outboxMessages, c => c, m => m.Id, (_, m) => m)
            .Select(m => new Handlers.OutboxMessage<T>(m, this))
            .ToArray();

        foreach (var retry in retries)
            AddRetry(retry, GetFailureRetryDelay(retry));
    }

    private TimeSpan GetFailureRetryDelay(Handlers.OutboxMessage<T> message)
    {
        var retryDelays = _options.RetryDelays;

        return retryDelays is []
            ? _options.FailureDelay
            : retryDelays[Math.Min(message.Attempt, retryDelays.Length - 1)];
    }

    public sealed record RetryRequest(Handlers.OutboxMessage<T> Message, DateTimeOffset? ScheduledAt);
}
