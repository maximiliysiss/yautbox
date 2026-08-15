using System;

namespace Yautbox.Tracing;

internal sealed class DefaultOutboxTracer : IOutboxTracer
{
    private static readonly IOutboxTraceScope Scope = new NoopTraceScope();

    public IOutboxTraceScope StartEnqueue(string identifier, int count) => Scope;
    public IOutboxTraceScope StartFetch(string identifier, int count) => Scope;
    public IOutboxTraceScope StartHandle(string identifier, int count) => Scope;
    public IOutboxTraceScope StartPersist(string identifier) => Scope;
    public IOutboxTraceScope StartCleanup(string identifier) => Scope;
    public IOutboxTraceScope StartCancellation(string identifier) => Scope;

    private sealed class NoopTraceScope : IOutboxTraceScope
    {
        public void SetTag(string name, object? value) { }
        public void SetFailed(Exception exception) { }
        public void Dispose() { }
    }
}
