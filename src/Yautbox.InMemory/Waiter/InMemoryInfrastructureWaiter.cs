using System.Threading;
using System.Threading.Tasks;
using Yautbox.Infrastructure.Waiter;

namespace Yautbox.InMemory.Waiter;

internal sealed class InMemoryInfrastructureWaiter : IInfrastructureReadinessWaiter
{
    public Task WaitAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
