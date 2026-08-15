using System;

namespace Yautbox.Tracing;

/// <summary>
/// Represents an outbox tracing span.
/// </summary>
public interface IOutboxTraceScope : IDisposable
{
    /// <summary>
    /// Adds an attribute to the current span.
    /// </summary>
    void SetTag(string name, object? value);

    /// <summary>
    /// Marks the current span as failed.
    /// </summary>
    void SetFailed(Exception exception);
}
