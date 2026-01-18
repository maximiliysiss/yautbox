using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Yautbox.Infrastructure.Waiter;
using Yautbox.Provider;

namespace Yautbox.Extensions.Builders.Outbox;

internal sealed class OutboxInfrastructureBuilder(IServiceCollection services) : IOutboxInfrastructureBuilder
{
    public IServiceCollection Services => services;

    public IOutboxInfrastructureBuilder SetProvider<T>(ServiceLifetime lifetime = ServiceLifetime.Scoped) where T : class, IOutboxProvider
    {
        services.TryAdd(ServiceDescriptor.Describe(typeof(IOutboxProvider), typeof(T), lifetime));
        return this;
    }

    public IOutboxInfrastructureBuilder SetWaiter<T>() where T : class, IInfrastructureReadinessWaiter
    {
        services.Decorate<IInfrastructureReadinessWaiter, T>();
        return this;
    }
}
