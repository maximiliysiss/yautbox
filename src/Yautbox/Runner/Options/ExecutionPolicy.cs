namespace Yautbox.Runner.Options;

/// <summary>
/// Specifies how outbox handler execution is scheduled.
/// </summary>
public enum ExecutionPolicy
{
    /// <summary>
    /// Non-blocking execution policy.
    /// </summary>
    Parallel,

    /// <summary>
    /// Blocking execution policy.
    /// </summary>
    Sequential
}
