using Yautbox.Infrastructure.Waiter;

namespace Yautbox.Postgres.Environment;

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
