using Microsoft.Extensions.DependencyInjection;
using Yautbox.Extensions.Builders.Outbox;
using Yautbox.InMemory.Infrastructure;
using Yautbox.InMemory.Options;
using Yautbox.InMemory.Provider;

namespace Yautbox.InMemory.Extensions;

public static class OutboxInfrastructureBuilderExtensions
{
    public static void UseInMemory(this IOutboxInfrastructureBuilder builder, InMemoryOutboxOptions? options = null)
    {
        builder
            .Services
            .AddSingleton(options ?? new InMemoryOutboxOptions())
            .AddSingleton<IDateTimeProvider, DateTimeProvider>();

        builder
            .SetProvider<InMemoryOutboxProvider>(ServiceLifetime.Singleton);
    }
}
