namespace Yautbox.InMemory.Options;

/// <summary>
/// Configures the in-memory outbox storage.
/// </summary>
public sealed class InMemoryOutboxOptions
{
    /// <summary>
    /// Capacity for inner queue. Default is 10000 per outbox handler
    /// </summary>
    public int Capacity { get; set; } = 10000;
}
