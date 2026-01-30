using System;

namespace Yautbox.Exceptions;

public sealed class HandlerAlreadyAddedException(Type type) : InvalidOperationException($"Handler for type '{type}' is already added");
