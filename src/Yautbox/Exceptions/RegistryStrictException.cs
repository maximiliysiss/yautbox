using System;

namespace Yautbox.Exceptions;

/// <summary>
/// Thrown when a type is not registered and the registry policy is strict.
/// </summary>
/// <param name="type">Type that was requested but not registered.</param>
public sealed class RegistryStrictException(Type type)
    : InvalidOperationException($"Registry strict mode is enabled and identifier for type '{type.FullName}' is not registered.");
