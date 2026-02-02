using Yautbox.Runner.Options;

namespace Yautbox.Postgres.Provider;

internal sealed class PostgresOutboxProviderOptions
{
    public OutboxDeletePolicy CancellingPolicy { get; set; }
}
