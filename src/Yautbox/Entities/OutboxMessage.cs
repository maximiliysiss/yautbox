using System;

namespace Yautbox.Entities;

/// <summary>
/// Represents an outbox message persisted for later processing.
/// </summary>
/// <param name="Id">Unique outbox message identifier.</param>
/// <param name="Payload">User payload to be handled.</param>
/// <param name="CreatedAt">Timestamp when the message was created.</param>
/// <param name="Attempt">Current delivery attempt number.</param>
/// <param name="ScheduledAt">Optional time when the message becomes visible for handling.</param>
public record OutboxMessage<T>(
    OutboxMessageId Id,
    T Payload,
    DateTimeOffset CreatedAt,
    int Attempt,
    DateTimeOffset? ScheduledAt);
