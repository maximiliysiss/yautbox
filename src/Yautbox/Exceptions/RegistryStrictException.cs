using System;

namespace Yautbox.Exceptions;

public sealed class RegistryStrictException(Type type)
    : InvalidOperationException($"Registry strict mode is enabled and identifier for type '{type.FullName}' is not registered.");
