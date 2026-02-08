using System;

namespace Yautbox.Runner.Options;

public interface IOutboxRunnerOptions
{
    /// <summary>
    /// Outbox handler key. Default is typeof(TPayload).AssemblyQualifiedName without version, culture, and public key token
    /// </summary>
    string? Identifier { get; }

    /// <summary>
    /// Delay between outbox cycles when there are no new records. The default value is 5 seconds + jitter
    /// </summary>
    TimeSpan PollDelay { get; }

    /// <summary>
    /// Count of outbox messages which will be handled. The default value is 1000
    /// </summary>
    int BufferSize { get; }

    /// <summary>
    /// Timeout to handle all messages in one buffer. The default value is 30 minutes
    /// </summary>
    TimeSpan HandleTimeout { get; }

    /// <summary>
    /// Is enabled this outbox handler or not? Default value is true
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Count of parallel workers inside a background job. Default value is 1
    /// WARNING: it can increase the count of database connections
    /// </summary>
    int WorkersCount { get; }

    /// <summary>
    /// Inner buffers for handling buffer in parallel. Default value is BufferSize
    /// </summary>
    int PerBufferCount => BufferSize;

    /// <summary>
    /// Outbox delete policy. Default is safe-delete
    /// </summary>
    DeletePolicy DeletePolicy { get; }

    /// <summary>
    /// Delay when outbox loop cycle failed. Default is 2 seconds + jitter
    /// </summary>
    TimeSpan FailureDelay { get; }

    /// <summary>
    /// Visibility timeout for processing messages. Messages being processed will not be visible to other processors for this duration.
    /// Default is 10 minutes
    /// </summary>
    TimeSpan Visibility { get; }

    /// <summary>
    /// Interval for cleanup old-handled messages. Default is null (aka off)
    /// </summary>
    TimeSpan? BackupInterval { get; }

    /// <summary>
    /// Execution policy for outbox handlers. Default is parallel
    /// </summary>
    ExecutionPolicy ExecutionPolicy { get; }

    /// <summary>
    /// Cancellation policy for outbox handlers. Default is safe-delete
    /// </summary>
    DeletePolicy CancellationPolicy { get; }
}
