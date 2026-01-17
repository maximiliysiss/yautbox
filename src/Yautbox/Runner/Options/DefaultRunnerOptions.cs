using System;

namespace Yautbox.Runner.Options;

internal sealed class DefaultRunnerOptions : IOutboxRunnerOptions
{
    public TimeSpan PollDelay { get; set; } = TimeSpan.FromSeconds(5);
    public int BufferSize { get; set; } = 1000;
    public TimeSpan HandleTimeout { get; set; } = TimeSpan.FromMinutes(30);
    public bool IsDisabled { get; set; } = false;
    public int WorkersCount { get; set; } = 1;
    public int PerBufferCount { get; set; } = 1000;
    public OutboxDeletePolicy DeletePolicy { get; set; } = OutboxDeletePolicy.Safe;
    public TimeSpan FailureDelay { get; set; } = TimeSpan.FromSeconds(2);
    public TimeSpan Visibility { get; set; } = TimeSpan.FromMinutes(10);
}
