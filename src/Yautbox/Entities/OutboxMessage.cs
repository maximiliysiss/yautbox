using System;

namespace Yautbox.Entities;

public record OutboxMessage<TPayload>(
    OutboxMessageId Id,
    TPayload Payload,
    DateTimeOffset CreatedAt,
    int Attempt,
    DateTimeOffset? ScheduledAt);
