using System;

namespace Yautbox.Runner;

public interface IOutboxRunnerOptions
{
    TimeSpan PollDelay { get; }
    int BufferSize { get; }
    TimeSpan HandleTimeout { get; }
    bool IsDisabled { get; }
}
