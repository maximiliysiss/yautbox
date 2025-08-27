namespace Yautbox.Entities;

public record OutboxMessage<TPayload>(OutboxMessageId Id, TPayload Payload);
