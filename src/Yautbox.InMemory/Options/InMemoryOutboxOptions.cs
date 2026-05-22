namespace Yautbox.InMemory.Options;

/// <summary>
/// Configures the in-memory outbox storage.
/// </summary>
public sealed class InMemoryOutboxOptions
{
    /// <summary>
    /// Gets or sets the maximum number of queued messages per outbox handler. The default is 10000.
    /// </summary>
    public int Capacity { get; set; } = 10000;
}
