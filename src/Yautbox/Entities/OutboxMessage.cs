using System;

namespace Yautbox.Entities;

public record OutboxMessage<T>(
    OutboxMessageId Id,
    T Payload,
    DateTimeOffset CreatedAt,
    int Attempt,
    DateTimeOffset? ScheduledAt);
