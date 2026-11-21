using System;
using System.Threading;
using System.Threading.Tasks;
using Yautbox.Runner.Options;

namespace Yautbox.Runner.Infrastructure;

internal sealed class DefaultPolicyFactory : IPolicyFactory
{
    private static readonly Task<IOutboxPolicyScope?> _task = Task.FromResult<IOutboxPolicyScope?>(EmptyScope.Instance);

    public Task<IOutboxPolicyScope?> TryCreateAsync(
        string identifier,
        ExecutionPolicy policy,
        TimeSpan timeout,
        CancellationToken cancellationToken)
        => _task;

    private sealed class EmptyScope : IOutboxPolicyScope
    {
        public static EmptyScope Instance { get; } = new();

        private EmptyScope()
        {
        }

        public CancellationToken HandleLostToken => CancellationToken.None;

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
