namespace Yautbox.Entities;

/// <summary>
/// Strongly typed identifier for an outbox message.
/// </summary>
[StronglyTypedId(
    backingType: StronglyTypedIdBackingType.Long,
    jsonConverter: StronglyTypedIdJsonConverter.SystemTextJson)]
public readonly partial struct OutboxMessageId;
