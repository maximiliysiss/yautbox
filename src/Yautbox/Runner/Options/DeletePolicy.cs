namespace Yautbox.Runner.Options;

/// <summary>
/// Defines how handled messages are removed from storage.
/// </summary>
public enum DeletePolicy
{
    /// <summary>
    /// Marks handled messages as deleted so they can be cleaned later.
    /// </summary>
    Safe,

    /// <summary>
    /// Permanently deletes handled messages immediately.
    /// </summary>
    Delete,
}
