using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Yautbox.Extensions.Outbox;
using Yautbox.InMemory.IntegrationTests.Shared.Fixture;
using Yautbox.InMemory.IntegrationTests.Shared.State;
using Yautbox.Services;

namespace Yautbox.InMemory.IntegrationTests.Cases;

[Collection(nameof(InMemoryOutboxIntegrationTestCollection))]
public class InMemoryOutboxIntegrationTests(InMemoryOutboxIntegrationTestFixture fixture)
{
    [Fact]
    public async Task MultipleHandlers_ShouldHandleMessagesForEachType()
    {
        // Arrange
        IntegrationTestState.Reset();
        using var scope = fixture.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IOutboxService>();

        // Act
        await service.HandleAsync(new IntegrationTestState.MessageA(10));
        await service.HandleAsync(new IntegrationTestState.MessageB(20));

        // Assert
        await WaitUntilAsync(
            condition: () => IntegrationTestState.MessageAValues.Count >= 1 &&
                             IntegrationTestState.MessageBValues.Count >= 1,
            timeout: TimeSpan.FromSeconds(2));

        IntegrationTestState.MessageAValues.Should().ContainSingle().Which.Should().Be(10);
        IntegrationTestState.MessageBValues.Should().ContainSingle().Which.Should().Be(20);
    }

    [Fact]
    public async Task ScheduledMessage_ShouldNotBeHandledBeforeScheduledTime()
    {
        // Arrange
        IntegrationTestState.Reset();
        using var scope = fixture.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IOutboxService>();
        var scheduledAt = DateTimeOffset.UtcNow.AddMilliseconds(200);

        // Act
        await service.HandleAsync(new IntegrationTestState.ScheduledMessage(5), scheduledAt);

        // Assert
        await Task.Delay(TimeSpan.FromMilliseconds(75));
        IntegrationTestState.ScheduledValues.Should().BeEmpty();

        await WaitUntilAsync(
            condition: () => IntegrationTestState.ScheduledValues.Count >= 1,
            timeout: TimeSpan.FromSeconds(2));

        IntegrationTestState.ScheduledValues.Should().ContainSingle().Which.Should().Be(5);
    }

    [Fact]
    public async Task CancelledMessage_ShouldNotBeHandled()
    {
        // Arrange
        IntegrationTestState.Reset();
        using var scope = fixture.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IOutboxService>();
        var scheduledAt = DateTimeOffset.UtcNow.AddMilliseconds(200);

        // Act
        var id = await service.HandleAsync(new IntegrationTestState.CancelMessage(99), scheduledAt);
        await service.CancelAsync(id, default);

        // Assert
        await Task.Delay(TimeSpan.FromMilliseconds(500));
        IntegrationTestState.CancelValues.Should().BeEmpty();
    }

    [Fact]
    public async Task RetryMessage_ShouldBeHandledTwiceWithIncrementedAttempt()
    {
        // Arrange
        IntegrationTestState.Reset();
        using var scope = fixture.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IOutboxService>();

        // Act
        await service.HandleAsync(new IntegrationTestState.RetryMessage(1));

        // Assert
        await WaitUntilAsync(
            condition: () => IntegrationTestState.RetryAttempts.Count >= 2,
            timeout: TimeSpan.FromSeconds(3));

        var attempts = IntegrationTestState.RetryAttempts.ToArray();
        attempts.Should().HaveCount(2);
        attempts.Should().Contain([0, 1]);
    }

    private static async Task WaitUntilAsync(Func<bool> condition, TimeSpan timeout)
    {
        var stopwatch = Stopwatch.StartNew();

        while (stopwatch.Elapsed < timeout)
        {
            if (condition())
                return;

            await Task.Delay(TimeSpan.FromMilliseconds(25));
        }

        throw new TimeoutException("Condition was not met within the allotted time.");
    }
}
