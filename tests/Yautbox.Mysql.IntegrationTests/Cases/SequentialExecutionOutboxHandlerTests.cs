using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Yautbox.Handlers;
using Yautbox.Mysql.IntegrationTests.Shared.Fixture;
using Yautbox.Services;

namespace Yautbox.Mysql.IntegrationTests.Cases;

[Collection(nameof(IntegrationTestCollection))]
public class SequentialExecutionOutboxHandlerTests(IntegrationTestFixture fixture)
{
    [Fact]
    public async Task Handler_ShouldProcessMessages_WithSequentialExecution()
    {
        // Arrange
        Handler.Reset();
        using var scope = fixture.Services.CreateScope();

        var service = scope.ServiceProvider.GetRequiredService<IOutboxService>();
        var values = Enumerable.Range(1, 10).ToArray();
        var messages = values.Select(value => new Message(value)).ToArray();

        // Act
        await service.HandleAsync(messages);

        await WaitForHandledCountAsync(values.Length, TimeSpan.FromSeconds(2));

        // Assert
        Handler.Values.Should().BeInAscendingOrder().And.BeEquivalentTo(values);
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
        private static List<int> _values = [];
        public static IReadOnlyCollection<int> Values => _values;

        public static void Reset() => _values = [];

        public Task HandleAsync(IEnumerable<OutboxMessage<Message>> messages, CancellationToken cancellationToken)
        {
            foreach (var message in messages)
                _values.Add(message.Payload.Value);

            return Task.CompletedTask;
        }
    }
}
