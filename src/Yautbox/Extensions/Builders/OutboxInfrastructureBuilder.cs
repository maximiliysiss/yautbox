using Yautbox.Infrastructure;
using Yautbox.Persistence;

namespace Yautbox.Extensions.Builders;

internal sealed class OutboxInfrastructureBuilder(IServiceCollection services) : IOutboxInfrastructureBuilder
{
    private bool _useDefaultReadinessWaiter = true;
    public IServiceCollection Services { get; } = services;

    public IOutboxInfrastructureBuilder SetOutboxRepository<TOutboxRepository>() where TOutboxRepository : class, IOutboxRepository
    {
        Services.TryAddScoped<IOutboxRepository, TOutboxRepository>();
        return this;
    }

    public IOutboxInfrastructureBuilder SetReadinessWaiter(IInfrastructureReadinessWaiter readinessWaiter)
    {
        Services.AddSingleton(readinessWaiter);
        _useDefaultReadinessWaiter = false;
        return this;
    }

    internal void Build()
    {
        if (_useDefaultReadinessWaiter)
            Services.AddSingleton<IInfrastructureReadinessWaiter>(new DefaultReadinessWaiter());
    }
}
