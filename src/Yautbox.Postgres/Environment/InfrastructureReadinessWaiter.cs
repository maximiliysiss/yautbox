using System.Threading;
using System.Threading.Tasks;
using Yautbox.Infrastructure.Waiter;

namespace Yautbox.Postgres.Environment;

internal class InfrastructureReadinessWaiter : IInfrastructureReadinessWaiter, ISynchronizer
{
    private readonly TaskCompletionSource _completionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public Task ReadyAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _completionSource.SetResult();
        return Task.CompletedTask;
    }

    public Task WaitAsync(CancellationToken cancellationToken) => _completionSource.Task.WaitAsync(cancellationToken);
}
