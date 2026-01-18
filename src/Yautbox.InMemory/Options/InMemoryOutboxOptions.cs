namespace Yautbox.InMemory.Options;

public sealed class InMemoryOutboxOptions
{
    /// <summary>
    /// Capacity for inner queue. Default is 1000 per outbox handler
    /// </summary>
    public int Capacity { get; set; } = 1000;
}
