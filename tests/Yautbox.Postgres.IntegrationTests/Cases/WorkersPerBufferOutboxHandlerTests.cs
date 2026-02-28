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
using Yautbox.Handlers;
using Yautbox.Postgres.IntegrationTests.Shared.Fixture;
using Yautbox.Services;
using Microsoft.Extensions.Logging;
namespace Yautbox.Postgres.IntegrationTests.Cases;

[Collection(nameof(IntegrationTestCollection))]
public class WorkersPerBufferOutboxHandlerTests(IntegrationTestFixture fixture)
{
    [Fact]
    public async Task Handler_ShouldProcessAllMessages_WithMultipleWorkers()
    {
        // Arrange
        Handler.Reset();
        using var scope = fixture.Services.CreateScope();

        var service = scope.ServiceProvider.GetRequiredService<IOutboxService>();
        var values = Enumerable.Range(1, 9).ToArray();
        var messages = values.Select(value => new Message(value)).ToArray();

        // Act
        await service.HandleAsync(messages);

        await WaitForHandledCountAsync(values.Length, TimeSpan.FromSeconds(2));

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
        private readonly ILogger<Handler> _logger;
        public Handler(ILogger<Handler> logger)
        {
            _logger = logger;
        }

        private static ConcurrentDictionary<int, byte> _values = new();
        public static IReadOnlyCollection<int> Values => [.. _values.Keys];

        public static void Reset()
        {
            _values = new ConcurrentDictionary<int, byte>();
        }

        public Task HandleAsync(IEnumerable<OutboxMessage<Message>> messages, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Handling messages.");
            foreach (var message in messages)
                _values.TryAdd(message.Payload.Value, 0);

            return Task.CompletedTask;
        }
    }
}
