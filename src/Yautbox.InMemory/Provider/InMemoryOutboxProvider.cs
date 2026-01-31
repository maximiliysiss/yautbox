using System.Collections.Concurrent;
using System.Transactions;
using Yautbox.Entities;
using Yautbox.InMemory.Collections;
using Yautbox.InMemory.Infrastructure;
using Yautbox.InMemory.Options;
using Yautbox.InMemory.Primitives;
using Yautbox.Provider;
using Yautbox.Runner.Options;
using Transaction = Yautbox.InMemory.Transactions.Transaction;

namespace Yautbox.InMemory.Provider;

internal sealed class InMemoryOutboxProvider : IOutboxProvider
{
    private long _index;

    private readonly ConcurrentDictionary<string, LimitedConcurrentDeque<object>> _inMemoryQueue = [];
    private readonly ConcurrentDictionary<OutboxMessageId, int> _enqueuedQueue = [];
    private readonly ConcurrentDictionary<string, DynamicCountdownEvent<OutboxMessageId>> _lockers = [];

    private readonly InMemoryOutboxOptions _options;
    private readonly IDateTimeProvider _dateTimeProvider;

    public InMemoryOutboxProvider(InMemoryOutboxOptions options, IDateTimeProvider dateTimeProvider)
    {
        _options = options;
        _dateTimeProvider = dateTimeProvider;
    }

    public Task<IReadOnlyCollection<OutboxMessage<T>>> GetAsync<T>(
        string identifier,
        int count,
        TimeSpan visibility,
        OutboxExecutionPolicy policy,
        CancellationToken cancellationToken)
    {
        if (!_inMemoryQueue.TryGetValue(identifier, out var inMemoryQueue))
            return Task.FromResult<IReadOnlyCollection<OutboxMessage<T>>>([]);

        DynamicCountdownEvent<OutboxMessageId>? locker = null;

        if (policy is OutboxExecutionPolicy.Sequential)
            locker = _lockers.GetOrAdd(identifier, _ => new DynamicCountdownEvent<OutboxMessageId>());

        locker?.Wait();

        var batch = new List<OutboxMessage<T>>(count);
        var ids = new List<OutboxMessageId>(count);

        while (count > 0 && inMemoryQueue.TryPopLeft(out var item))
        {
            var message = (OutboxMessage<T>)item;

            if (!_enqueuedQueue.ContainsKey(message.Id))
                continue;

            batch.Add(message);
            ids.Add(message.Id);

            count--;
        }

        var rescheduledMessages = batch
            .Select(m => (Message: m, Attempt: _enqueuedQueue.GetValueOrDefault(m.Id)))
            .OrderByDescending(c => c.Message.Id)
            .ToArray();

        _ = Task.Run(
            function: () => RescheduleAsync(rescheduledMessages),
            cancellationToken: CancellationToken.None);

        locker?.Acquire(ids);

        return Task.FromResult<IReadOnlyCollection<OutboxMessage<T>>>(batch);

        async Task RescheduleAsync(IEnumerable<(OutboxMessage<T> Message, int Attempt)> messages)
        {
            await Task.Delay(visibility, CancellationToken.None);

            foreach (var (message, attempt) in messages)
            {
                if (!_enqueuedQueue.TryGetValue(message.Id, out var current))
                    return;

                if (current != attempt)
                    return;

                inMemoryQueue.PushLeft(message);
            }
        }
    }

    public Task<IReadOnlyCollection<OutboxMessageId>> AddAsync<T>(
        string identifier,
        IReadOnlyCollection<OutboxMessage<T>> messages,
        CancellationToken cancellationToken)
    {
        if (messages.Count is 0)
            return Task.FromResult<IReadOnlyCollection<OutboxMessageId>>([]);

        var inMemoryQueue = _inMemoryQueue.GetOrAdd(
            key: identifier,
            valueFactory: _ => new LimitedConcurrentDeque<object>(_options.Capacity));

        var transaction = Transaction.Current();

        var localIds = new List<OutboxMessageId>(messages.Count);

        foreach (var message in messages)
        {
            var local = message;

            if (local.Id == OutboxMessageId.Empty)
            {
                var id = new OutboxMessageId(Interlocked.Increment(ref _index));
                local = local with { Id = id };
            }

            localIds.Add(local.Id);

            if (transaction is null)
            {
                EnqueueItem(local);
                continue;
            }

            TransactionCompletedEventHandler? handler = null;
            handler = void (_, e) => PushItemToQueue(e, local, handler);

            transaction.TransactionCompleted += handler;
        }

        return Task.FromResult<IReadOnlyCollection<OutboxMessageId>>(localIds);

        async Task ScheduleAsync(OutboxMessage<T> local, TimeSpan delay)
        {
            await Task.Delay(delay, CancellationToken.None);
            inMemoryQueue.PushRight(local);
        }

        void PushItemToQueue(TransactionEventArgs args, OutboxMessage<T> local, TransactionCompletedEventHandler? handler)
        {
            try
            {
                if (args.Transaction?.TransactionInformation.Status is not TransactionStatus.Committed)
                    return;

                EnqueueItem(local);
            }
            finally
            {
                transaction.TransactionCompleted -= handler;
            }
        }

        void EnqueueItem(OutboxMessage<T> local)
        {
            _enqueuedQueue.TryAdd(local.Id, 0);

            if (local.ScheduledAt is not null)
            {
                var delay = local.ScheduledAt.Value - _dateTimeProvider.GetNow();

                if (delay > TimeSpan.Zero)
                {
                    _ = Task.Run(
                        function: () => ScheduleAsync(local, delay),
                        cancellationToken: CancellationToken.None);

                    return;
                }
            }

            inMemoryQueue.PushRight(local);
        }
    }

    public Task CancelAsync(
        IReadOnlyCollection<OutboxMessageId> ids,
        CancellationToken cancellationToken)
        => DeleteAsync(ids, OutboxDeletePolicy.Delete, cancellationToken);

    public Task DeleteAsync(
        IReadOnlyCollection<OutboxMessageId> ids,
        OutboxDeletePolicy policy,
        CancellationToken cancellationToken)
    {
        foreach (var id in ids)
            _enqueuedQueue.TryRemove(id, out _);

        foreach (var (_, ev) in _lockers)
            ev.Release(ids);

        return Task.CompletedTask;
    }

    public Task CleanAsync(DateTimeOffset olderThan, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task RetryAsync<T>(
        string identifier,
        IReadOnlyCollection<OutboxMessage<T>> messages,
        CancellationToken cancellationToken)
    {
        foreach (var (outboxMessageId, _, _, currentAttempt, _) in messages)
            _enqueuedQueue.TryUpdate(key: outboxMessageId, newValue: currentAttempt, comparisonValue: currentAttempt - 1);

        return AddAsync(identifier, messages, cancellationToken);
    }
}
