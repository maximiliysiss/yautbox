using System.Collections.Concurrent;
using System.Threading.Channels;
using Yautbox.Entities;
using Yautbox.InMemory.Infrastructure;
using Yautbox.InMemory.Options;
using Yautbox.Provider;
using Yautbox.Runner.Options;

namespace Yautbox.InMemory.Provider;

internal sealed class InMemoryOutboxProvider : IOutboxProvider
{
    private long _index;

    private readonly ConcurrentDictionary<Type, Channel<object>> _inMemoryQueue = [];
    private readonly ConcurrentDictionary<OutboxMessageId, int> _enqueuedQueue = [];

    private readonly InMemoryOutboxOptions _options;
    private readonly IDateTimeProvider _dateTimeProvider;

    public InMemoryOutboxProvider(InMemoryOutboxOptions options, IDateTimeProvider dateTimeProvider)
    {
        _options = options;
        _dateTimeProvider = dateTimeProvider;
    }

    public Task<IReadOnlyCollection<OutboxMessage<T>>> GetAsync<T>(
        int count,
        TimeSpan visibility,
        CancellationToken cancellationToken)
    {
        if (!_inMemoryQueue.TryGetValue(typeof(T), out var inMemoryQueue))
            return Task.FromResult<IReadOnlyCollection<OutboxMessage<T>>>([]);

        var reader = inMemoryQueue.Reader;
        var writer = inMemoryQueue.Writer;

        var batch = new List<OutboxMessage<T>>(count);

        while (count > 0 && reader.TryRead(out var item))
        {
            var message = (OutboxMessage<T>)item;

            if (!_enqueuedQueue.TryGetValue(message.Id, out var attempt))
                continue;

            batch.Add(message);
            count--;

            _ = Task.Run(
                function: () => RescheduleAsync(message, attempt),
                cancellationToken: CancellationToken.None);
        }

        return Task.FromResult<IReadOnlyCollection<OutboxMessage<T>>>(batch);

        async Task RescheduleAsync(OutboxMessage<T> message, int attempt)
        {
            await Task.Delay(visibility, CancellationToken.None);

            if (!_enqueuedQueue.TryGetValue(message.Id, out var current))
                return;

            if (current != attempt)
                return;

            await writer.WriteAsync(message, CancellationToken.None);
        }
    }

    public async Task<IReadOnlyCollection<OutboxMessageId>> AddAsync<T>(
        IReadOnlyCollection<OutboxMessage<T>> messages,
        CancellationToken cancellationToken)
    {
        if (messages.Count is 0)
            return [];

        var inMemoryQueue = _inMemoryQueue.GetOrAdd(
            key: typeof(T),
            valueFactory: _ => Channel.CreateBounded<object>(_options.Capacity));

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
            _enqueuedQueue.TryAdd(local.Id, 0);

            if (local.ScheduledAt is not null)
            {
                var delay = local.ScheduledAt.Value - _dateTimeProvider.GetNow();

                if (delay > TimeSpan.Zero)
                {
                    _ = Task.Run(
                        function: () => ScheduleAsync(local, delay),
                        cancellationToken: CancellationToken.None);

                    continue;
                }
            }

            await inMemoryQueue.Writer.WriteAsync(
                item: local,
                cancellationToken: cancellationToken);
        }

        return localIds;

        async Task ScheduleAsync(OutboxMessage<T> local, TimeSpan delay)
        {
            await Task.Delay(delay, CancellationToken.None);
            await inMemoryQueue.Writer.WriteAsync(local, CancellationToken.None);
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

        return Task.CompletedTask;
    }

    public Task RetryAsync<T>(
        IReadOnlyCollection<OutboxMessage<T>> messages,
        CancellationToken cancellationToken)
    {
        foreach (var (outboxMessageId, _, _, currentAttempt, _) in messages)
            _enqueuedQueue.TryUpdate(key: outboxMessageId, newValue: currentAttempt, comparisonValue: currentAttempt - 1);

        return AddAsync(messages, cancellationToken);
    }
}
