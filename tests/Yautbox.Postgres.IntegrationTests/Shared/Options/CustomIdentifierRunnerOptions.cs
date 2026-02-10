using System;
using Yautbox.Runner.Options;

namespace Yautbox.Postgres.IntegrationTests.Shared.Options;

public sealed class CustomIdentifierRunnerOptions : IOutboxRunnerOptions
{
    public const string IdentifierValue = "yautbox-inmemory-custom-identifier";

    public string? Identifier { get; set; } = IdentifierValue;
    public TimeSpan PollDelay { get; set; } = TimeSpan.FromMilliseconds(50);
    public int BufferSize { get; set; } = 64;
    public TimeSpan HandleTimeout { get; set; } = TimeSpan.FromSeconds(5);
    public bool IsEnabled { get; set; } = true;
    public int WorkersCount { get; set; } = 1;
    public int PerBufferCount { get; set; } = 32;
    public DeletePolicy DeletePolicy { get; set; } = DeletePolicy.Safe;
    public TimeSpan FailureDelay { get; set; } = TimeSpan.FromMilliseconds(100);
    public TimeSpan Visibility { get; set; } = TimeSpan.FromMilliseconds(200);
    public TimeSpan? BackupInterval { get; set; }
    public ExecutionPolicy ExecutionPolicy { get; set; } = ExecutionPolicy.Parallel;
    public DeletePolicy CancellationPolicy { get; set; } = DeletePolicy.Safe;
}
