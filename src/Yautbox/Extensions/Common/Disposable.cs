using System;
using System.Threading.Tasks;

namespace Yautbox.Extensions.Common;

internal static class Disposable
{
    public static readonly IAsyncDisposable Empty = new EmptyDisposable();
    public static readonly Task<IAsyncDisposable> EmptyTask = Task.FromResult(Empty);

    private class EmptyDisposable : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
