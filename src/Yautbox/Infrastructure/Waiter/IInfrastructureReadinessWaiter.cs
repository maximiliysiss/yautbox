using System.Threading;
using System.Threading.Tasks;

namespace Yautbox.Infrastructure.Waiter;

public interface IInfrastructureReadinessWaiter
{
    Task WaitAsync(CancellationToken cancellationToken);
}
