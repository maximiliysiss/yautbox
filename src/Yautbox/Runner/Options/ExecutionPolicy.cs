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
    /// Requires a provider policy scope before processing messages for the same identifier, so only one worker across
    /// all instances processes a cycle at a time. Workers that do not get the scope within
    /// <see cref="IOutboxRunnerOptions{TPayload}.PolicyTimeout"/> skip the cycle until the next poll.
    /// The scope is provided by the provider's <see cref="Infrastructure.IPolicyFactory"/>: Postgres uses an advisory lock,
    /// InMemory an in-process lock. Without a provider policy factory no lock is taken.
    /// </summary>
    Sequential
}
