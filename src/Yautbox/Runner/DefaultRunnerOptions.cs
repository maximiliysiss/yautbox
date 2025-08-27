using System;

namespace Yautbox.Runner;

internal sealed class DefaultRunnerOptions : IOutboxRunnerOptions
{
    public TimeSpan PollDelay => TimeSpan.FromSeconds(10);
    public int BufferSize => 1000;
    public TimeSpan HandleTimeout => TimeSpan.FromMinutes(2);
    public bool IsDisabled => false;
}
