using System;
using Yautbox.Entities;
using Yautbox.Runner.Infrastructure;

namespace Yautbox.Handlers;

/// <summary>
/// Wraps an outbox message during handler execution.
/// </summary>
/// <typeparam name="T">Payload type carried by the message.</typeparam>
public sealed class OutboxMessage<T>
{
    private readonly OutboxRunnerContext<T> _context;
    private readonly Entities.OutboxMessage<T> _message;

    internal OutboxMessageId Id => _message.Id;

    /// <summary>
    /// Gets the message payload.
    /// </summary>
    public T Payload => _message.Payload;

    /// <summary>
    /// Gets the current attempt number.
    /// </summary>
    public int Attempt => _message.Attempt;

    /// <summary>
    /// Gets the creation timestamp of the message.
    /// </summary>
    public DateTimeOffset CreatedAt => _message.CreatedAt;

    internal OutboxMessage(Entities.OutboxMessage<T> message, OutboxRunnerContext<T> context)
    {
        _context = context;
        _message = message;
    }

    /// <summary>
    /// Requests a retry at a specific time.
    /// </summary>
    /// <param name="at">Time when the message should be retried.</param>
    public void Retry(DateTimeOffset? at = null) => _context.AddRetry(this, at);

    /// <summary>
    /// Requests a retry after the specified delay.
    /// </summary>
    /// <param name="delay">Delay before the message should be retried.</param>
    public void Retry(TimeSpan? delay = null) => _context.AddRetry(this, delay);
}
