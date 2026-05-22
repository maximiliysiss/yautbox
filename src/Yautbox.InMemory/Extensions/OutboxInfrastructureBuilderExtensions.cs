using Microsoft.Extensions.DependencyInjection;
using Yautbox.Extensions.Builders.Outbox;
using Yautbox.InMemory.Infrastructure;
using Yautbox.InMemory.Options;
using Yautbox.InMemory.Provider;
using Yautbox.InMemory.Waiter;

namespace Yautbox.InMemory.Extensions;

/// <summary>
/// Extension methods for configuring the in-memory outbox infrastructure.
/// </summary>
public static class OutboxInfrastructureBuilderExtensions
{
    /// <summary>
    /// Configures the in-memory outbox provider and its options.
    /// </summary>
    /// <param name="builder">Infrastructure builder to configure.</param>
    /// <param name="options">Optional in-memory options override.</param>
    public static void UseInMemory(this IOutboxInfrastructureBuilder builder, InMemoryOutboxOptions? options = null)
    {
        builder
            .Services
            .AddSingleton(options ?? new InMemoryOutboxOptions())
            .AddSingleton<IDateTimeProvider, DateTimeProvider>();

        builder
            .SetProvider<InMemoryOutboxProvider>(ServiceLifetime.Singleton)
            .SetWaiter<InMemoryInfrastructureWaiter>();
    }
}
