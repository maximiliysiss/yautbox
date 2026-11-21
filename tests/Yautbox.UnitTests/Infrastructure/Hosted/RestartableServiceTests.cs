using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using Yautbox.Infrastructure.Hosted;
using Yautbox.Infrastructure.Sleep;

namespace Yautbox.UnitTests.Infrastructure.Hosted;

public class RestartableServiceTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldNotRestart_WhenInterruptLoopExceptionIsThrown()
    {
        // Arrange
        var service = new ThrowingService(() => new InterruptLoopException("invalid options"));

        // Act
        await service.StartAsync(CancellationToken.None);
        var act = () => service.ExecuteTask!;

        // Assert
        await act.Should().ThrowAsync<InterruptLoopException>();
        service.Executions.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldRestart_WhenOtherExceptionIsThrown()
    {
        // Arrange
        var service = new ThrowingService(() => new InvalidOperationException("transient"));

        // Act
        await service.StartAsync(CancellationToken.None);
        await WaitUntilAsync(() => service.Executions >= 3);
        await service.StopAsync(CancellationToken.None);

        // Assert
        service.Executions.Should().BeGreaterThanOrEqualTo(3);
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (!condition())
            await Task.Delay(10, timeout.Token);
    }

    private sealed class ThrowingService(Func<Exception> exception)
        : RestartableService(NullLogger.Instance, new NoDelay())
    {
        private int _executions;

        public int Executions => Volatile.Read(ref _executions);

        protected override Task ExecuteAsync(CancellationTokenSource reloadTokenSource)
        {
            Interlocked.Increment(ref _executions);
            throw exception();
        }
    }

    private sealed class NoDelay : ISleepDurationProvider
    {
        public TimeSpan GetSleepDelay(int attempt) => TimeSpan.Zero;
    }
}
