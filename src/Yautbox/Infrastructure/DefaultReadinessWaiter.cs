using System.Threading;
using System.Threading.Tasks;

namespace Yautbox.Infrastructure;

public class DefaultReadinessWaiter : IInfrastructureReadinessWaiter
{
    public Task WaitAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
