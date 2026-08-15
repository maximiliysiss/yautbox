namespace Yautbox.Tracing;

/// <summary>
/// Creates tracing spans for outbox operations.
/// </summary>
public interface IOutboxTracer
{
    IOutboxTraceScope StartEnqueue(string identifier, int count);
    IOutboxTraceScope StartFetch(string identifier, int count);
    IOutboxTraceScope StartHandle(string identifier, int count);
    IOutboxTraceScope StartPersist(string identifier);
    IOutboxTraceScope StartCleanup(string identifier);
    IOutboxTraceScope StartCancellation(string identifier);
}
