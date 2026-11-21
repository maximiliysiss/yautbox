using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Yautbox.Entities;
using Yautbox.Extensions.Logger;
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

    private readonly ILogger _logger;
    private readonly IDateTimeProvider _dateTimeProvider;

    private readonly IReadOnlyCollection<OutboxMessage<T>> _outboxMessages;

    private readonly IOutboxRunnerOptions<T> _options;

    public OutboxRunnerContext(
        ILogger logger,
        IDateTimeProvider dateTimeProvider,
        IReadOnlyCollection<OutboxMessage<T>> outboxMessages,
        IOutboxRunnerOptions<T> options)
    {
        _logger = logger;
        _dateTimeProvider = dateTimeProvider;
        _outboxMessages = outboxMessages;
        _options = options;
        _success = [.. outboxMessages.Select(c => c.Id)];
    }

    public IReadOnlyCollection<OutboxMessageId> RetryExceeded => _retryExceeded.AsReadOnly();

    public bool IsFailed { get; private set; }

    public bool IsSuccess => _success.Count > 0;

    public void AddRetry(Handlers.OutboxMessage<T> message, DateTimeOffset? at = null)
    {
        // A postponed retry lets later messages overtake this one, which breaks Sequential ordering.
        // A message that exceeded RetryCount is deleted instead, so it does not reorder anything.
        if (_options.ExecutionPolicy is ExecutionPolicy.Sequential
            && at > _dateTimeProvider.GetNow()
            && !IsRetryCountExceeded(message))
        {
            _logger.OutboxSequentialRetryReordering(
                messageId: message.Id.Value,
                type: typeof(T).Name,
                scheduledAt: at);
        }

        AddRetryCore(message, at);
    }

    public void AddRetry(Handlers.OutboxMessage<T> message, TimeSpan? delay = null)
        => AddRetry(message, ToScheduledAt(delay));

    public void Fail()
    {
        IsFailed = true;

        var retries = _success
            .Join(_outboxMessages, c => c, m => m.Id, (_, m) => m)
            .Select(m => new Handlers.OutboxMessage<T>(m, this))
            .ToArray();

        foreach (var retry in retries)
            AddRetryCore(retry, ToScheduledAt(GetFailureRetryDelay(retry)));
    }

    private void AddRetryCore(Handlers.OutboxMessage<T> message, DateTimeOffset? at)
    {
        _success.Remove(message.Id);

        if (IsRetryCountExceeded(message))
        {
            _retryExceeded.Add(message.Id);
            return;
        }

        _retries.Add(new RetryRequest(message, at));
    }

    private bool IsRetryCountExceeded(Handlers.OutboxMessage<T> message)
        => message.Attempt + 1 > _options.RetryCount;

    private DateTimeOffset? ToScheduledAt(TimeSpan? delay)
        => delay is { } value ? _dateTimeProvider.GetNow().Add(value) : null;

    private TimeSpan? GetFailureRetryDelay(Handlers.OutboxMessage<T> message)
    {
        var retryDelays = _options.RetryDelays;

        if (retryDelays is not [])
            return retryDelays[Math.Min(message.Attempt, retryDelays.Length - 1)];

        // Sequential processing must keep the order: a failed message is retried in the next cycle,
        // so later messages cannot overtake it while it waits for FailureDelay.
        return _options.ExecutionPolicy is ExecutionPolicy.Sequential
            ? null
            : _options.FailureDelay;
    }

    public sealed record RetryRequest(Handlers.OutboxMessage<T> Message, DateTimeOffset? ScheduledAt);
}
