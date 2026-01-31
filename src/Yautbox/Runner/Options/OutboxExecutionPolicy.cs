namespace Yautbox.Runner.Options;

public enum OutboxExecutionPolicy
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
