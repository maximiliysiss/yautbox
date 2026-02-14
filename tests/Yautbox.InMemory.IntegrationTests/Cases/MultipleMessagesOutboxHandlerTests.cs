using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;
using Yautbox.Handlers;
using Yautbox.InMemory.IntegrationTests.Shared.Fixture;
using Yautbox.Services;

namespace Yautbox.InMemory.IntegrationTests.Cases;

[Collection(nameof(IntegrationTestCollection))]
public class MultipleMessagesOutboxHandlerTests(IntegrationTestFixture fixture, ITestOutputHelper output)
{
    [Fact]
    public async Task Handler_ShouldHandleAllMessages()
    {
        // Arrange
        Handler.Reset();
        using var scope = fixture.Services.CreateScope();

        var service = scope.ServiceProvider.GetRequiredService<IOutboxService>();
        var values = Enumerable.Range(1, 8).ToArray();
        var messages = values.Select(value => new Message(value)).ToArray();

        // Act
        await service.HandleAsync(messages);

        await WaitForHandledCountAsync(values.Length, TimeSpan.FromSeconds(2));

        // Assert
        Handler.Values.Should().BeEquivalentTo(values);
    }

    [Fact]
    public async Task Handler_ShouldHandleLargeBatch_AndLogDuration()
    {
        // Arrange
        Handler.Reset();
        using var scope = fixture.Services.CreateScope();

        var service = scope.ServiceProvider.GetRequiredService<IOutboxService>();
        var values = Enumerable.Range(1, 2000).ToArray();
        var messages = values.Select(value => new Message(value)).ToArray();

        // Act
        var stopwatch = Stopwatch.StartNew();
        await service.HandleAsync(messages);
        await WaitForHandledCountAsync(values.Length, TimeSpan.FromSeconds(10));
        stopwatch.Stop();

        output.WriteLine($"Handled {values.Length} messages in {stopwatch.Elapsed}.");

        // Assert
        Handler.Values.Should().BeEquivalentTo(values);
    }

    private static async Task WaitForHandledCountAsync(int expected, TimeSpan timeout)
    {
        var stopwatch = Stopwatch.StartNew();
        while (stopwatch.Elapsed < timeout)
        {
            if (Handler.Values.Count == expected)
                return;

            await Task.Delay(TimeSpan.FromMilliseconds(20));
        }

        throw new TimeoutException($"Expected {expected} handled messages, but saw {Handler.Values.Count}.");
    }

    public sealed record Message(int Value);

    public sealed class Handler : IOutboxHandler<Message>
    {
        private static ConcurrentDictionary<int, byte> _values = new();
        public static IReadOnlyCollection<int> Values => [.. _values.Keys];

        public static void Reset() => _values = new ConcurrentDictionary<int, byte>();

        public Task HandleAsync(IEnumerable<OutboxMessage<Message>> messages, CancellationToken cancellationToken)
        {
            foreach (var message in messages)
                _values.TryAdd(message.Payload.Value, 0);

            return Task.CompletedTask;
        }
    }
}
