using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Yautbox.Handlers;

namespace Yautbox.InMemory.IntegrationTests.Shared.State;

public static class IntegrationTestState
{
    public static ConcurrentQueue<int> MessageAValues { get; private set; } = new();
    public static ConcurrentQueue<int> MessageBValues { get; private set; } = new();
    public static ConcurrentQueue<int> ScheduledValues { get; private set; } = new();
    public static ConcurrentQueue<int> CancelValues { get; private set; } = new();
    public static ConcurrentQueue<int> RetryAttempts { get; private set; } = new();

    public static void Reset()
    {
        MessageAValues = new ConcurrentQueue<int>();
        MessageBValues = new ConcurrentQueue<int>();
        ScheduledValues = new ConcurrentQueue<int>();
        CancelValues = new ConcurrentQueue<int>();
        RetryAttempts = new ConcurrentQueue<int>();
    }

    public sealed record MessageA(int Value);
    public sealed record MessageB(int Value);
    public sealed record ScheduledMessage(int Value);
    public sealed record CancelMessage(int Value);
    public sealed record RetryMessage(int Value);

    public sealed class HandlerA : IOutboxHandler<MessageA>
    {
        public Task HandleAsync(IEnumerable<OutboxMessage<MessageA>> messages, CancellationToken cancellationToken)
        {
            foreach (var message in messages)
                MessageAValues.Enqueue(message.Payload.Value);

            return Task.CompletedTask;
        }
    }

    public sealed class HandlerB : IOutboxHandler<MessageB>
    {
        public Task HandleAsync(IEnumerable<OutboxMessage<MessageB>> messages, CancellationToken cancellationToken)
        {
            foreach (var message in messages)
                MessageBValues.Enqueue(message.Payload.Value);

            return Task.CompletedTask;
        }
    }

    public sealed class ScheduledHandler : IOutboxHandler<ScheduledMessage>
    {
        public Task HandleAsync(IEnumerable<OutboxMessage<ScheduledMessage>> messages, CancellationToken cancellationToken)
        {
            foreach (var message in messages)
                ScheduledValues.Enqueue(message.Payload.Value);

            return Task.CompletedTask;
        }
    }

    public sealed class CancelHandler : IOutboxHandler<CancelMessage>
    {
        public Task HandleAsync(IEnumerable<OutboxMessage<CancelMessage>> messages, CancellationToken cancellationToken)
        {
            foreach (var message in messages)
                CancelValues.Enqueue(message.Payload.Value);

            return Task.CompletedTask;
        }
    }

    public sealed class RetryHandler : IOutboxHandler<RetryMessage>
    {
        public Task HandleAsync(IEnumerable<OutboxMessage<RetryMessage>> messages, CancellationToken cancellationToken)
        {
            foreach (var message in messages)
            {
                RetryAttempts.Enqueue(message.Attempt);

                if (message.Attempt == 0)
                    message.Retry(System.TimeSpan.FromMilliseconds(50));
            }

            return Task.CompletedTask;
        }
    }
}
