using System;
using System.Threading;
using System.Threading.Tasks;

namespace Yautbox.Metrics;

internal sealed class DefaultMetricsHandler : IMetricsHandler
{
    public ValueTask AddedAsync(string identifier, int count, CancellationToken cancellationToken) => ValueTask.CompletedTask;
    public ValueTask CanceledAsync(string identifier, int count, CancellationToken cancellationToken) => ValueTask.CompletedTask;

    public ValueTask HandledAsync(string identifier, int count, TimeSpan elapsed, CancellationToken cancellationToken)
        => ValueTask.CompletedTask;

    public ValueTask RetriedAsync(string identifier, int count, CancellationToken cancellationToken) => ValueTask.CompletedTask;
    public ValueTask DeletedAsync(string identifier, int count, CancellationToken cancellationToken) => ValueTask.CompletedTask;
    public ValueTask CleanedInAsync(string identifier, TimeSpan elapsed, CancellationToken cancellationToken) => ValueTask.CompletedTask;
    public ValueTask ReadInAsync(string identifier, TimeSpan elapsed, CancellationToken cancellationToken) => ValueTask.CompletedTask;
    public ValueTask ErrorsAsync(string identifier, int count, CancellationToken cancellationToken) => ValueTask.CompletedTask;
}
