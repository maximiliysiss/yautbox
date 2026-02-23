namespace Yautbox.Runner.Options;

/// <summary>
/// Defines how handled messages are removed from storage.
/// </summary>
public enum DeletePolicy
{
    /// <summary>
    /// Safe delete policy for handleded messages.
    /// </summary>
    Safe,

    /// <summary>
    /// Delete policy for handleded messages.
    /// </summary>
    Delete,
}
