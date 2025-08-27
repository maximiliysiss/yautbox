using System.Threading;
using System.Threading.Tasks;

namespace Yautbox.Infrastructure;

public interface IInfrastructureReadinessWaiter
{
    Task WaitAsync(CancellationToken cancellationToken);
}
