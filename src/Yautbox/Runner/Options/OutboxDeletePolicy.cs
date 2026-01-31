namespace Yautbox.Runner.Options;

public enum OutboxDeletePolicy
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
