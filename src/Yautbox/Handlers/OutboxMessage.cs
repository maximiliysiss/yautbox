using System;
using Yautbox.Entities;
using Yautbox.Runner;
using Yautbox.Runner.Infrastructure;

namespace Yautbox.Handlers;

public sealed class OutboxMessage<T>
{
    private readonly OutboxRunnerContext<T> _context;
    private readonly Entities.OutboxMessage<T> _message;

    internal OutboxMessageId Id => _message.Id;
    public T Payload => _message.Payload;
    public int Attempt => _message.Attempt;
    public DateTimeOffset CreatedAt => _message.CreatedAt;

    internal OutboxMessage(Entities.OutboxMessage<T> message, OutboxRunnerContext<T> context)
    {
        _context = context;
        _message = message;
    }

    public void Retry(DateTimeOffset? at = null) => _context.AddRetry(this, at);
    public void Retry(TimeSpan? delay = null) => _context.AddRetry(this, delay);
}
