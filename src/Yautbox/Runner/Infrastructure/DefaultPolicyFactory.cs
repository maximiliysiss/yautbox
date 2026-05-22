using System;
using System.Threading;
using System.Threading.Tasks;
using Yautbox.Extensions.Common;
using Yautbox.Runner.Options;

namespace Yautbox.Runner.Infrastructure;

internal sealed class DefaultPolicyFactory : IPolicyFactory
{
    public Task<IAsyncDisposable> CreateAsync(
        string identifier,
        ExecutionPolicy policy,
        TimeSpan timeout,
        CancellationToken cancellationToken)
        => Disposable.EmptyTask;
}
