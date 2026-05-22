using System;

namespace Yautbox.Runner.Options;

internal sealed class InnerDefaultRunnerOptions : IOutboxRunnerOptions
{
    public string? Identifier { get; set; }
    public TimeSpan PollDelay { get; set; } = TimeSpan.FromSeconds(5);
    public int BufferSize { get; set; } = 1000;
    public TimeSpan HandleTimeout { get; set; } = TimeSpan.FromMinutes(30);
    public bool IsEnabled { get; set; } = true;
    public int WorkersCount { get; set; } = 1;
    public int PerBufferCount { get; set; } = 1000;
    public DeletePolicy DeletePolicy { get; set; } = DeletePolicy.Safe;
    public TimeSpan FailureDelay { get; set; } = TimeSpan.FromSeconds(2);
    public TimeSpan Visibility { get; set; } = TimeSpan.FromMinutes(10);
    public TimeSpan? BackupInterval { get; set; }
    public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromDays(1);
    public ExecutionPolicy ExecutionPolicy { get; set; } = ExecutionPolicy.Parallel;
    public DeletePolicy CancellationPolicy { get; set; } = DeletePolicy.Safe;
    public ScopeLifetime ScopeLifetime { get; set; } = ScopeLifetime.PerBatch;
}
