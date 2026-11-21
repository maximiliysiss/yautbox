using System;

namespace Yautbox.Runner.Options;

/// <summary>
/// Provides default runner option values for handlers that only need to specify <see cref="IOutboxRunnerOptions{TPayload}.BufferSize"/>.
/// </summary>
/// <typeparam name="TPayload">Payload type handled by the handler these options apply to.</typeparam>
public interface ISimpleRunnerOptions<TPayload> : IOutboxRunnerOptions<TPayload>
{
    string? IOutboxRunnerOptions<TPayload>.Identifier => null;
    TimeSpan IOutboxRunnerOptions<TPayload>.PollDelay => TimeSpan.FromSeconds(5);
    TimeSpan IOutboxRunnerOptions<TPayload>.HandleTimeout => TimeSpan.FromMinutes(55);
    bool IOutboxRunnerOptions<TPayload>.IsEnabled => true;
    int IOutboxRunnerOptions<TPayload>.WorkersCount => 1;
    int IOutboxRunnerOptions<TPayload>.PerBufferCount => BufferSize;
    DeletePolicy IOutboxRunnerOptions<TPayload>.DeletePolicy => DeletePolicy.Safe;
    TimeSpan IOutboxRunnerOptions<TPayload>.FailureDelay => TimeSpan.FromSeconds(2);
    TimeSpan IOutboxRunnerOptions<TPayload>.Visibility => TimeSpan.FromHours(1);
    TimeSpan? IOutboxRunnerOptions<TPayload>.BackupInterval => null;
    TimeSpan IOutboxRunnerOptions<TPayload>.CleanupInterval => TimeSpan.FromDays(1);
    ExecutionPolicy IOutboxRunnerOptions<TPayload>.ExecutionPolicy => ExecutionPolicy.Parallel;
    DeletePolicy IOutboxRunnerOptions<TPayload>.CancellationPolicy => DeletePolicy.Safe;
    ScopeLifetime IOutboxRunnerOptions<TPayload>.ScopeLifetime => ScopeLifetime.PerBatch;
    TimeSpan IOutboxRunnerOptions<TPayload>.PolicyTimeout => TimeSpan.FromSeconds(5);
}
