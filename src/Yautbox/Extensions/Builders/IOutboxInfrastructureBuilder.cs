using Yautbox.Infrastructure;
using Yautbox.Persistence;

namespace Yautbox.Extensions.Builders;

public interface IOutboxInfrastructureBuilder
{
    IServiceCollection Services { get; }

    IOutboxInfrastructureBuilder SetOutboxRepository<TOutboxRepository>()
        where TOutboxRepository : class, IOutboxRepository;

    IOutboxInfrastructureBuilder SetReadinessWaiter(IInfrastructureReadinessWaiter readinessWaiter);
}
