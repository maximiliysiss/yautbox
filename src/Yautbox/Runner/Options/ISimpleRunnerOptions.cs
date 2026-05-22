using System;

namespace Yautbox.Runner.Options;

/// <summary>
/// Provides default runner option values for handlers that only need to specify <see cref="IOutboxRunnerOptions.BufferSize"/>.
/// </summary>
public interface ISimpleRunnerOptions : IOutboxRunnerOptions
{
    string? IOutboxRunnerOptions.Identifier => null;
    TimeSpan IOutboxRunnerOptions.PollDelay => TimeSpan.FromSeconds(5);
    TimeSpan IOutboxRunnerOptions.HandleTimeout => TimeSpan.FromMinutes(55);
    bool IOutboxRunnerOptions.IsEnabled => true;
    int IOutboxRunnerOptions.WorkersCount => 1;
    int IOutboxRunnerOptions.PerBufferCount => BufferSize;
    DeletePolicy IOutboxRunnerOptions.DeletePolicy => DeletePolicy.Safe;
    TimeSpan IOutboxRunnerOptions.FailureDelay => TimeSpan.FromSeconds(2);
    TimeSpan IOutboxRunnerOptions.Visibility => TimeSpan.FromHours(1);
    TimeSpan? IOutboxRunnerOptions.BackupInterval => null;
    TimeSpan IOutboxRunnerOptions.CleanupInterval => TimeSpan.FromDays(1);
    ExecutionPolicy IOutboxRunnerOptions.ExecutionPolicy => ExecutionPolicy.Parallel;
    DeletePolicy IOutboxRunnerOptions.CancellationPolicy => DeletePolicy.Safe;
    ScopeLifetime IOutboxRunnerOptions.ScopeLifetime => ScopeLifetime.PerBatch;
    TimeSpan IOutboxRunnerOptions.PolicyTimeout => TimeSpan.FromMinutes(55);
}
