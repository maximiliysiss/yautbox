using System.Threading;
using System.Threading.Tasks;
using Yautbox.Infrastructure.Waiter;

namespace Yautbox.Mssql.Environment;

internal class InfrastructureReadinessWaiter : IInfrastructureReadinessWaiter, ISynchronizer
{
    private readonly TaskCompletionSource _completionSource = new();

    public Task ReadyAsync(CancellationToken cancellationToken)
    {
        _completionSource.SetResult();
        return Task.CompletedTask;
    }

    public Task WaitAsync(CancellationToken cancellationToken) => _completionSource.Task;
}
