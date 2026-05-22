namespace Yautbox.Registy;

/// <summary>
/// Defines how the outbox registry handles payload types that were not registered explicitly.
/// </summary>
public enum OutboxRegistryPolicy
{
    /// <summary>
    /// Throws <see cref="Yautbox.Exceptions.RegistryStrictException"/> for unregistered payload types.
    /// </summary>
    Strict,

    /// <summary>
    /// Uses the default identifier and cancellation policy for unregistered payload types.
    /// </summary>
    Lenient,
}
