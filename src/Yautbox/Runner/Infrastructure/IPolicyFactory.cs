using System;
using System.Threading;
using System.Threading.Tasks;
using Yautbox.Runner.Options;

namespace Yautbox.Runner.Infrastructure;

public interface IPolicyFactory
{
    Task<IAsyncDisposable> CreateAsync(string identifier, ExecutionPolicy policy, CancellationToken cancellationToken);
}
