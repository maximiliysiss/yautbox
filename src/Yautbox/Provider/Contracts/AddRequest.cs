using Yautbox.Entities;

namespace Yautbox.Provider.Contracts;

/// <summary>
///
/// </summary>
/// <param name="Identifier"></param>
/// <param name="Message"></param>
/// <typeparam name="T"></typeparam>
public sealed record AddRequest<T>(string Identifier, OutboxMessage<T> Message);
