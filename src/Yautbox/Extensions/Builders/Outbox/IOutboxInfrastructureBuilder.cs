using Microsoft.Extensions.DependencyInjection;
using Yautbox.Infrastructure.Waiter;
using Yautbox.Provider;

namespace Yautbox.Extensions.Builders.Outbox;

public interface IOutboxInfrastructureBuilder
{
    IServiceCollection Services { get; }
    IOutboxInfrastructureBuilder SetProvider<T>() where T : class, IOutboxProvider;
    IOutboxInfrastructureBuilder SetReadinessWaiter<T>() where T : class, IInfrastructureReadinessWaiter;
}
