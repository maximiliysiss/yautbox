namespace Yautbox.Registy;

/// <summary>
/// Represents the policies available for the behavior of the Outbox registry.
/// </summary>
public enum OutboxRegistryPolicy
{
    /// <summary>
    /// Represents a strict policy for the Outbox registry.
    /// When this policy is active, any attempt to access an unregistered type
    /// will result in a <see cref="Yautbox.Exceptions.RegistryStrictException"/> being thrown.
    /// This ensures that all relevant types are explicitly registered before usage to avoid
    /// unexpected behaviors.
    /// </summary>
    Strict,

    /// <summary>
    /// Represents a lenient policy for the Outbox registry.
    /// When this policy is active, attempts to access an unregistered type
    /// will not raise exceptions. This allows for dynamic handling of types
    /// without mandatory pre-registration, potentially accommodating greater flexibility
    /// but at the risk of reduced type safety.
    /// </summary>
    Lenient,
}
