using System;

namespace Yautbox.Runner.Options;

internal sealed class DefaultRunnerOptions : IOutboxRunnerOptions
{
    public string? Identifier => null;
    public TimeSpan PollDelay { get; } = TimeSpan.FromSeconds(5);
    public int BufferSize => 1000;
    public TimeSpan HandleTimeout { get; } = TimeSpan.FromMinutes(30);
    public bool IsEnabled => true;
    public int WorkersCount => 1;
    public int PerBufferCount => 1000;
    public DeletePolicy DeletePolicy => DeletePolicy.Safe;
    public TimeSpan FailureDelay { get; } = TimeSpan.FromSeconds(2);
    public TimeSpan Visibility { get; } = TimeSpan.FromMinutes(10);
    public TimeSpan? BackupInterval => null;
    public TimeSpan CleanupInterval { get; } = TimeSpan.FromDays(1);
    public ExecutionPolicy ExecutionPolicy => ExecutionPolicy.Parallel;
    public DeletePolicy CancellationPolicy => DeletePolicy.Safe;
    public ScopeLifetime ScopeLifetime => ScopeLifetime.PerBatch;
}
