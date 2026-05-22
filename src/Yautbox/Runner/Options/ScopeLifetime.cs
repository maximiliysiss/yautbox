namespace Yautbox.Runner.Options;

/// <summary>
/// Defines how long the dependency injection scope for an outbox handler is reused.
/// </summary>
public enum ScopeLifetime
{
    /// <summary>
    /// Reuses one handler scope for the whole runner session.
    /// </summary>
    PerSession,

    /// <summary>
    /// Reuses one handler scope for one worker group.
    /// </summary>
    PerGroup,

    /// <summary>
    /// Creates a handler scope for each handled batch.
    /// </summary>
    PerBatch,
}
