using Microsoft.Extensions.DependencyInjection;
using Yautbox.Infrastructure.Waiter;
using Yautbox.Provider;
using Yautbox.Runner.Infrastructure;

namespace Yautbox.Extensions.Builders.Outbox;

public interface IOutboxInfrastructureBuilder
{
    IServiceCollection Services { get; }
    IOutboxInfrastructureBuilder SetProvider<T>(ServiceLifetime lifetime = ServiceLifetime.Scoped) where T : class, IOutboxProvider;
    IOutboxInfrastructureBuilder SetWaiter<T>() where T : class, IInfrastructureReadinessWaiter;
    IOutboxInfrastructureBuilder SetPolicy<T>() where T : class, IPolicyFactory;
}
