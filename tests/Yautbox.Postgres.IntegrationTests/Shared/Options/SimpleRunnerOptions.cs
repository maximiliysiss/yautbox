using Yautbox.Runner.Options;

namespace Yautbox.Postgres.IntegrationTests.Shared.Options;

public sealed class SimpleRunnerOptions : ISimpleRunnerOptions
{
    public bool IsEnabled { get; set; }
    public int BufferSize { get; set; }
}
