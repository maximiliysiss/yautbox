using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using Yautbox.InMemory.Policy;
using Yautbox.Runner.Options;

namespace Yautbox.InMemory.UnitTests.Policy;

public class InMemoryPolicyFactoryTests
{
    private static readonly TimeSpan ShortTimeout = TimeSpan.FromMilliseconds(50);

    [Fact]
    public async Task TryCreateAsync_ShouldReturnNull_WhenSequentialScopeIsHeld()
    {
        // Arrange
        var factory = new InMemoryPolicyFactory();
        await using var first = await factory.TryCreateAsync("id", ExecutionPolicy.Sequential, ShortTimeout, CancellationToken.None);

        // Act
        var second = await factory.TryCreateAsync("id", ExecutionPolicy.Sequential, ShortTimeout, CancellationToken.None);

        // Assert
        first.Should().NotBeNull();
        second.Should().BeNull();
    }

    [Fact]
    public async Task TryCreateAsync_ShouldAcquire_AfterSequentialScopeIsReleased()
    {
        // Arrange
        var factory = new InMemoryPolicyFactory();
        var first = await factory.TryCreateAsync("id", ExecutionPolicy.Sequential, ShortTimeout, CancellationToken.None);
        await first!.DisposeAsync();
        await first.DisposeAsync(); // a double dispose must not release the lock twice

        // Act
        await using var second = await factory.TryCreateAsync("id", ExecutionPolicy.Sequential, ShortTimeout, CancellationToken.None);
        var third = await factory.TryCreateAsync("id", ExecutionPolicy.Sequential, ShortTimeout, CancellationToken.None);

        // Assert
        second.Should().NotBeNull();
        third.Should().BeNull();
    }

    [Fact]
    public async Task TryCreateAsync_ShouldNotShareLocks_BetweenIdentifiers()
    {
        // Arrange
        var factory = new InMemoryPolicyFactory();
        await using var first = await factory.TryCreateAsync("a", ExecutionPolicy.Sequential, ShortTimeout, CancellationToken.None);

        // Act
        await using var second = await factory.TryCreateAsync("b", ExecutionPolicy.Sequential, ShortTimeout, CancellationToken.None);

        // Assert
        second.Should().NotBeNull();
    }

    [Fact]
    public async Task TryCreateAsync_ShouldNotLock_WhenParallel()
    {
        // Arrange
        var factory = new InMemoryPolicyFactory();
        await using var first = await factory.TryCreateAsync("id", ExecutionPolicy.Parallel, ShortTimeout, CancellationToken.None);

        // Act
        await using var second = await factory.TryCreateAsync("id", ExecutionPolicy.Parallel, ShortTimeout, CancellationToken.None);

        // Assert
        second.Should().NotBeNull();
        second!.HandleLostToken.CanBeCanceled.Should().BeFalse();
    }
}
