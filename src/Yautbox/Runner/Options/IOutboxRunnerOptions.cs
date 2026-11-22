using System;

namespace Yautbox.Runner.Options;

/// <summary>
/// Defines configuration options for outbox handler execution of payload type <typeparamref name="TPayload"/>.
/// </summary>
/// <typeparam name="TPayload">Payload type handled by the handler these options apply to.</typeparam>
public interface IOutboxRunnerOptions<TPayload>
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
    /// Gets the timeout for fetching and handling one buffer of messages. When it expires, fetching is cancelled
    /// or the handler is cancelled and its messages are retried. The default is 55 minutes.
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
    /// Messages are split into chunks after being grouped by <see cref="PartitionKey"/> when it is set.
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
    /// Gets the maximum number of retries for one message. A <see langword="null"/> value means infinite retries.
    /// </summary>
    int? RetryCount => null;

    /// <summary>
    /// Gets retry delays by attempt. When the current attempt is greater than the array length, the last delay is reused.
    /// When empty, failed messages are retried after <see cref="FailureDelay"/>, or in the next cycle without delay
    /// under <see cref="ExecutionPolicy.Sequential"/> to preserve message order.
    /// </summary>
    TimeSpan[] RetryDelays => [];

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
    /// Gets the time to wait for the execution policy scope to become available. When it is not acquired in time,
    /// the cycle is skipped until the next <see cref="PollDelay"/>. The default is 5 seconds.
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

    /// <summary>
    /// Selects the partition a message belongs to. The default is <see langword="null"/>: the messages of a cycle are not partitioned.
    /// When set, the messages fetched in one cycle are grouped by the returned key before being split into
    /// <see cref="PerBufferCount"/> chunks, so a single <c>HandleAsync</c> call only receives messages with equal keys.
    /// Keys are compared with <see cref="object.Equals(object)"/>, so return a value type, a string or a record;
    /// messages whose key is <see langword="null"/> form one group of their own.
    /// </summary>
    /// <remarks>
    /// The chunks of all groups are handled concurrently, so a cycle makes one <c>HandleAsync</c> call per chunk —
    /// up to <see cref="BufferSize"/> concurrent calls when every message has a distinct key.
    /// Partitioning does not order messages: a group larger than <see cref="PerBufferCount"/> is still split into
    /// concurrent chunks.
    /// With <see cref="ExecutionPolicy.Parallel"/> messages with the same key fetched by other workers or
    /// instances are handled in parallel with this cycle.
    /// With <see cref="ExecutionPolicy.Sequential"/> the combination is intended: only one buffer is processed
    /// across all instances at a time, and the key adds parallelism inside that buffer. Use it to speed up
    /// sequential processing when the handler logic allows messages with different keys to be handled concurrently.
    /// The cycle ends when its slowest chunk does, and <see cref="HandleTimeout"/> applies to the cycle as a whole:
    /// one slow partition delays the next cycle for every key, and under <see cref="ExecutionPolicy.Sequential"/> keeps
    /// the execution scope held until it finishes.
    /// With <see cref="ScopeLifetime.PerGroup"/> or <see cref="ScopeLifetime.PerSession"/> all chunks of a cycle share
    /// one handler instance and its scoped dependencies, which are then called concurrently and must be thread-safe.
    /// <see cref="ScopeLifetime.PerBatch"/> gives every chunk its own scope and handler.
    /// The delegate runs for every fetched message outside the handler's error handling: if it throws, the whole
    /// cycle fails and its messages stay locked until <see cref="Visibility"/> expires.
    /// </remarks>
    Func<TPayload, object?>? PartitionKey => null;
}
