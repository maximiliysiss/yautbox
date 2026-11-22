using System;
using Microsoft.Extensions.Logging.Abstractions;
using Yautbox.Entities;
using Yautbox.Infrastructure.DateTime;
using Yautbox.Runner.Infrastructure;
using Yautbox.Runner.Options;

namespace Yautbox.Testing;

/// <summary>
/// Builds <see cref="Handlers.OutboxMessage{T}"/> instances for unit tests of an
/// <see cref="Handlers.IOutboxHandler{T}"/>, without storage, a host or a runner.
/// </summary>
public static class OutboxMessage
{
    /// <summary>
    /// Creates a message carrying <paramref name="payload"/>, as the outbox runner would deliver it to a handler.
    /// </summary>
    /// <remarks>
    /// The message is detached from any storage and has its own isolated processing context:
    /// <see cref="Handlers.OutboxMessage{T}.Retry(DateTimeOffset?)"/> and
    /// <see cref="Handlers.OutboxMessage{T}.Retry(TimeSpan?)"/> are accepted, but their effect cannot be observed.
    /// </remarks>
    /// <typeparam name="T">Payload type of the message.</typeparam>
    /// <param name="payload">Payload returned by <see cref="Handlers.OutboxMessage{T}.Payload"/>.</param>
    /// <param name="attempt">
    /// Value of <see cref="Handlers.OutboxMessage{T}.Attempt"/>; <c>0</c>, the first delivery, by default.
    /// </param>
    /// <param name="createdAt">
    /// Value of <see cref="Handlers.OutboxMessage{T}.CreatedAt"/>; when <see langword="null"/>, the current UTC time.
    /// </param>
    /// <returns>A message ready to be passed to <see cref="Handlers.IOutboxHandler{T}.HandleAsync"/>.</returns>
    public static Handlers.OutboxMessage<T> Of<T>(T payload, int attempt = 0, DateTimeOffset? createdAt = null)
    {
        var dateTimeProvider = new DateTimeProvider();

        var message = new OutboxMessage<T>(
            Id: OutboxMessageId.Empty,
            Payload: payload,
            Attempt: attempt,
            ScheduledAt: null,
            CreatedAt: createdAt ?? dateTimeProvider.GetNow());

        var context = new OutboxRunnerContext<T>(
            logger: NullLogger.Instance,
            dateTimeProvider: dateTimeProvider,
            outboxMessages: [message],
            options: new DefaultRunnerOptions<T>());

        return new Handlers.OutboxMessage<T>(message, context);
    }
}
