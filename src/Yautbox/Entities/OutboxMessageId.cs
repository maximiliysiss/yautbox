namespace Yautbox.Entities;

[StronglyTypedId(
    backingType: StronglyTypedIdBackingType.Long,
    jsonConverter: StronglyTypedIdJsonConverter.SystemTextJson | StronglyTypedIdJsonConverter.SystemTextJson)]
public readonly partial struct OutboxMessageId;
