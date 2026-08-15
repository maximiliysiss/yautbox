using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Yautbox.Infrastructure.Waiter;
using Yautbox.Metrics;
using Yautbox.Provider;
using Yautbox.Registy;
using Yautbox.Runner.Infrastructure;
using Yautbox.Tracing;

namespace Yautbox.Extensions.Builders.Outbox;

internal sealed class OutboxInfrastructureBuilder(IServiceCollection services) : IOutboxInfrastructureBuilder
{
    public IServiceCollection Services => services;

    public IOutboxInfrastructureBuilder SetProvider<T>(ServiceLifetime lifetime = ServiceLifetime.Scoped) where T : class, IOutboxProvider
    {
        services.TryAdd(ServiceDescriptor.Describe(typeof(IOutboxProvider), typeof(T), lifetime));
        return this;
    }

    public IOutboxInfrastructureBuilder SetWaiter<T>(ServiceLifetime lifetime = ServiceLifetime.Singleton)
        where T : class, IInfrastructureReadinessWaiter
    {
        services.TryAdd(ServiceDescriptor.Describe(typeof(IInfrastructureReadinessWaiter), typeof(T), lifetime));
        return this;
    }

    public IOutboxInfrastructureBuilder SetPolicy<T>(ServiceLifetime lifetime = ServiceLifetime.Scoped) where T : class, IPolicyFactory
    {
        services.TryAdd(ServiceDescriptor.Describe(typeof(IPolicyFactory), typeof(T), lifetime));
        return this;
    }

    public IOutboxInfrastructureBuilder SetPrefix(string prefix)
    {
        Services
            .AddOptions<OutboxRegistryOptions>()
            .Configure(opt => opt.Prefix = prefix);

        return this;
    }

    public IOutboxInfrastructureBuilder SetRegistryPolicy(OutboxRegistryPolicy policy)
    {
        Services
            .AddOptions<OutboxRegistryOptions>()
            .Configure(opt => opt.Policy = policy);

        return this;
    }

    public IOutboxInfrastructureBuilder SetMetrics<T>() where T : IMetricsHandler
    {
        Services.TryAdd(ServiceDescriptor.Describe(typeof(IMetricsHandler), typeof(T), ServiceLifetime.Singleton));
        return this;
    }

    public IOutboxInfrastructureBuilder SetTracing<T>() where T : class, IOutboxTracer
    {
        Services.TryAddSingleton<IOutboxTracer, T>();
        return this;
    }
}
