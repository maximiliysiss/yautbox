using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Yautbox.Infrastructure.Waiter;
using Yautbox.Provider;

namespace Yautbox.Extensions.Builders.Outbox;

internal sealed class OutboxInfrastructureBuilder(IServiceCollection services) : IOutboxInfrastructureBuilder
{
    public IServiceCollection Services => services;

    public IOutboxInfrastructureBuilder SetProvider<T>() where T : class, IOutboxProvider
    {
        services.TryAddScoped<IOutboxProvider, T>();
        return this;
    }

    public IOutboxInfrastructureBuilder SetReadinessWaiter<T>() where T : class, IInfrastructureReadinessWaiter
    {
        services.Decorate<IInfrastructureReadinessWaiter, T>();
        return this;
    }
}
