using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using Yautbox.InMemory.Collections;

namespace Yautbox.InMemory.UnitTests.Collections;

public class LimitedConcurrentDequeTest
{
    [Fact]
    public void PushRight_ShouldEnqueueInFifoOrder()
    {
        // Arrange
        var deque = new LimitedConcurrentDeque<int>(capacity: 2);

        // Act
        deque.PushRight(1);
        deque.PushRight(2);

        var first = deque.TryPopLeft(out var firstValue);
        var second = deque.TryPopLeft(out var secondValue);

        // Assert
        first.Should().BeTrue();
        second.Should().BeTrue();
        firstValue.Should().Be(1);
        secondValue.Should().Be(2);
    }

    [Fact]
    public void PushLeft_ShouldPlaceItemAtFront()
    {
        // Arrange
        var deque = new LimitedConcurrentDeque<int>(capacity: 2);

        // Act
        deque.PushRight(1);
        deque.PushLeft(2);

        var first = deque.TryPopLeft(out var firstValue);
        var second = deque.TryPopLeft(out var secondValue);

        // Assert
        first.Should().BeTrue();
        second.Should().BeTrue();
        firstValue.Should().Be(2);
        secondValue.Should().Be(1);
    }

    [Fact]
    public async Task PushRight_ShouldBlock_WhenCapacityReached_UntilPop()
    {
        // Arrange
        var deque = new LimitedConcurrentDeque<int>(capacity: 1);

        // Act
        deque.PushRight(1);

        var secondPush = Task.Run(() => deque.PushRight(2));
        var completed = await Task.WhenAny(secondPush, Task.Delay(100));

        // Assert
        completed.Should().NotBeSameAs(secondPush);

        deque.TryPopLeft(out _).Should().BeTrue();

        await secondPush;
        deque.TryPopLeft(out var secondValue).Should().BeTrue();
        secondValue.Should().Be(2);
    }

    [Fact]
    public void TryPopLeft_ShouldReturnFalse_WhenEmpty()
    {
        // Arrange
        var deque = new LimitedConcurrentDeque<string>(capacity: 1);

        // Act
        var result = deque.TryPopLeft(out var value);

        // Assert
        result.Should().BeFalse();
        value.Should().BeNull();
    }
}
