using System;

namespace Yautbox.Runner.Options;

/// <summary>
/// Defines configuration options for outbox handler execution.
/// </summary>
public interface IOutboxRunnerOptions
{
    /// <summary>
    /// Gets the outbox handler key. The default is the payload type assembly-qualified name without version, culture, and public key token.
    /// </summary>
    string? Identifier { get; }

    /// <summary>
    /// Gets the delay between polling cycles when there are no new records. The default is 5 seconds plus jitter.
    /// </summary>
    TimeSpan PollDelay { get; }

    /// <summary>
    /// Gets the maximum number of outbox messages read in one polling cycle. The default is 1000.
    /// </summary>
    int BufferSize { get; }

    /// <summary>
    /// Gets the timeout for handling one buffer of messages. The default is 55 minutes.
    /// </summary>
    TimeSpan HandleTimeout { get; }

    /// <summary>
    /// Gets a value indicating whether this outbox handler is enabled. The default is <see langword="true"/>.
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Gets the number of parallel workers inside the background job. The default is 1.
    /// </summary>
    /// <remarks>Increasing this value can increase the number of provider connections.</remarks>
    int WorkersCount { get; }

    /// <summary>
    /// Gets the maximum number of messages passed to one handler call. The default is <see cref="BufferSize"/>.
    /// </summary>
    int PerBufferCount => BufferSize;

    /// <summary>
    /// Gets the delete policy for successfully handled messages. The default is <see cref="DeletePolicy.Safe"/>.
    /// </summary>
    DeletePolicy DeletePolicy { get; }

    /// <summary>
    /// Gets the delay after a failed outbox loop cycle. The default is 2 seconds plus jitter.
    /// </summary>
    TimeSpan FailureDelay { get; }

    /// <summary>
    /// Gets the visibility timeout for messages being processed. The default is 1 hour.
    /// </summary>
    TimeSpan Visibility { get; }

    /// <summary>
    /// Gets the time-to-live for safe-deleted messages. A <see langword="null"/> value disables cleanup.
    /// </summary>
    TimeSpan? BackupInterval { get; }

    /// <summary>
    /// Gets the interval between cleanup cycles. The default is 1 day.
    /// </summary>
    TimeSpan CleanupInterval { get; }

    /// <summary>
    /// Gets the execution policy for outbox handlers. The default is <see cref="ExecutionPolicy.Parallel"/>.
    /// </summary>
    ExecutionPolicy ExecutionPolicy { get; }

    /// <summary>
    /// Gets the timeout for acquiring or holding the execution policy scope. The default is 55 minutes.
    /// </summary>
    TimeSpan PolicyTimeout { get; }

    /// <summary>
    /// Gets the cancellation policy for explicitly canceled messages. The default is <see cref="DeletePolicy.Safe"/>.
    /// </summary>
    DeletePolicy CancellationPolicy { get; }

    /// <summary>
    /// Gets the lifetime of the dependency injection scope used to resolve the handler. The default is <see cref="ScopeLifetime.PerBatch"/>.
    /// </summary>
    ScopeLifetime ScopeLifetime { get; }
}
