namespace Yautbox.Runner.Options;

/// <summary>
/// Specifies how outbox handler execution is scheduled.
/// </summary>
public enum ExecutionPolicy
{
    /// <summary>
    /// Allows workers to process messages without acquiring a sequential policy scope.
    /// </summary>
    Parallel,

    /// <summary>
    /// Requires a provider policy scope before processing messages for the same identifier.
    /// </summary>
    Sequential
}
