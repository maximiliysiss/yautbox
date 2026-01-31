using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using Yautbox.InMemory.Primitives;

namespace Yautbox.InMemory.UnitTests.Primitives;

public class DynamicCountdownEventTests
{
    [Fact]
    public void Acquire_ShouldAddDistinctElements()
    {
        // Arrange
        var countdownEvent = new DynamicCountdownEvent<int>();

        // Act
        countdownEvent.Acquire([1, 2, 2, 3]);

        // Assert
        var elements = GetElements(countdownEvent);
        elements.Should().HaveCount(3);
        elements.Should().BeEquivalentTo([1, 2, 3]);
    }

    [Fact]
    public void Release_ShouldRemoveExistingElements()
    {
        // Arrange
        var countdownEvent = new DynamicCountdownEvent<int>();
        countdownEvent.Acquire([1, 2, 3]);

        // Act
        countdownEvent.Release([2, 3]);

        // Assert
        var elements = GetElements(countdownEvent);
        elements.Should().ContainSingle().Which.Should().Be(1);
    }

    [Fact]
    public void Wait_ShouldReturnImmediately_WhenEmpty()
    {
        // Arrange
        var countdownEvent = new DynamicCountdownEvent<int>();

        // Act
        countdownEvent.Wait();

        // Assert
        GetElements(countdownEvent).Should().BeEmpty();
    }

    [Fact]
    public async Task Wait_ShouldBlockUntilRelease()
    {
        // Arrange
        var countdownEvent = new DynamicCountdownEvent<int>();
        countdownEvent.Acquire([1, 2]);

        // Act
        var waitTask = Task.Run(countdownEvent.Wait);
        var completedBeforeRelease = await Task.WhenAny(waitTask, Task.Delay(100));

        countdownEvent.Release([1]);
        var completedAfterPartialRelease = await Task.WhenAny(waitTask, Task.Delay(100));

        countdownEvent.Release([2]);
        var completedAfterRelease = await Task.WhenAny(waitTask, Task.Delay(1000));

        // Assert
        completedBeforeRelease.Should().NotBeSameAs(waitTask);
        completedAfterPartialRelease.Should().NotBeSameAs(waitTask);
        completedAfterRelease.Should().BeSameAs(waitTask);
    }

    [Fact]
    public async Task Wait_ShouldHandleStandardScenario()
    {
        // Arrange
        var countdownEvent = new DynamicCountdownEvent<int>();

        // Act
        countdownEvent.Wait();

        var secondWaitTask = Task.Run(countdownEvent.Wait);
        var secondWaitTaskCompleted = await Task.WhenAny(secondWaitTask, Task.Delay(100));

        countdownEvent.Acquire([1, 2]);
        countdownEvent.Release([1, 2]);

        // Act
        secondWaitTaskCompleted.Should().NotBeSameAs(secondWaitTask);
        await secondWaitTask;
    }

    private static ICollection<int> GetElements(DynamicCountdownEvent<int> countdownEvent)
    {
        var field = typeof(DynamicCountdownEvent<int>)
            .GetField("_elements", BindingFlags.Instance | BindingFlags.NonPublic);

        field.Should().NotBeNull();

        var dictionary = (ConcurrentDictionary<int, byte>)field!.GetValue(countdownEvent)!;

        return dictionary.Keys;
    }
}
