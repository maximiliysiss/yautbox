using System;
using Yautbox.Runner.Options;

namespace Yautbox.InMemory.IntegrationTests.Shared.Options;

public sealed class WorkersRunnerOptions : IOutboxRunnerOptions
{
    public string? Identifier { get; set; }
    public TimeSpan PollDelay { get; set; } = TimeSpan.FromMilliseconds(50);
    public int BufferSize { get; set; } = 6;
    public TimeSpan HandleTimeout { get; set; } = TimeSpan.FromSeconds(5);
    public bool IsEnabled { get; set; } = true;
    public int WorkersCount { get; set; } = 3;
    public int PerBufferCount { get; set; } = 2;
    public DeletePolicy DeletePolicy { get; set; } = DeletePolicy.Safe;
    public TimeSpan FailureDelay { get; set; } = TimeSpan.FromMilliseconds(100);
    public TimeSpan Visibility { get; set; } = TimeSpan.FromMilliseconds(200);
    public TimeSpan? BackupInterval { get; set; }
    public OutboxExecutionPolicy ExecutionPolicy { get; set; } = OutboxExecutionPolicy.Parallel;
    public DeletePolicy CancellationPolicy { get; set; } = DeletePolicy.Safe;
}
