using System;

namespace Yautbox.Exceptions;

/// <summary>
/// Thrown when a handler for a payload type is registered more than once.
/// </summary>
/// <param name="type">Payload type whose handler is already registered.</param>
public sealed class HandlerAlreadyAddedException(Type type) : InvalidOperationException($"Handler for type '{type}' is already added");
