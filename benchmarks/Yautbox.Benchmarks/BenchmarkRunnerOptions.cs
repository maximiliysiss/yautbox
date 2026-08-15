using Yautbox.Runner.Options;

namespace Yautbox.Benchmarks;

internal sealed class BenchmarkRunnerOptions : IOutboxRunnerOptions
{
    public string? Identifier => null;
    public TimeSpan PollDelay { get; init; } = TimeSpan.FromMilliseconds(10);
    public int BufferSize { get; init; }
    public TimeSpan HandleTimeout => TimeSpan.FromMinutes(30);
    public bool IsEnabled => true;
    public int WorkersCount { get; init; }
    public int PerBufferCount { get; init; }
    public DeletePolicy DeletePolicy => DeletePolicy.Delete;
    public TimeSpan FailureDelay => TimeSpan.FromMilliseconds(50);
    public TimeSpan Visibility => TimeSpan.FromMinutes(30);
    public TimeSpan? BackupInterval => null;
    public TimeSpan CleanupInterval => TimeSpan.FromDays(1);
    public ExecutionPolicy ExecutionPolicy => ExecutionPolicy.Parallel;
    public TimeSpan PolicyTimeout => TimeSpan.FromMinutes(30);
    public DeletePolicy CancellationPolicy => DeletePolicy.Delete;
    public ScopeLifetime ScopeLifetime => ScopeLifetime.PerBatch;
}
