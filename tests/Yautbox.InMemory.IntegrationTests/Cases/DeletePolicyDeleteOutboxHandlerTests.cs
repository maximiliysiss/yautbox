using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Yautbox.Handlers;
using Yautbox.InMemory.IntegrationTests.Shared.Extensions;
using Yautbox.InMemory.IntegrationTests.Shared.Fixture;
using Yautbox.Services;
using Microsoft.Extensions.Logging;
namespace Yautbox.InMemory.IntegrationTests.Cases;

[Collection(nameof(IntegrationTestCollection))]
public class DeletePolicyDeleteOutboxHandlerTests(IntegrationTestFixture fixture)
{
    [Fact]
    public async Task Handler_ShouldNotReprocess_WhenDeletePolicyIsDelete()
    {
        // Arrange
        Handler.Reset();
        using var scope = fixture.Services.CreateScope();

        var service = scope.ServiceProvider.GetRequiredService<IOutboxService>();

        // Act
        await service.HandleAsync(message: new Message(31));

        await WaitForHandledCountAsync(1, TimeSpan.FromSeconds(2));
        await Task.Delay(TimeSpan.FromMilliseconds(500));

        // Assert
        Handler.CallCount.Should().Be(1);
    }

    private static async Task WaitForHandledCountAsync(int expected, TimeSpan timeout)
    {
        var stopwatch = Stopwatch.StartNew();
        while (stopwatch.Elapsed < timeout)
        {
            if (Handler.CallCount == expected)
                return;

            await Task.Delay(TimeSpan.FromMilliseconds(20));
        }

        throw new TimeoutException($"Expected {expected} handled messages, but saw {Handler.CallCount}.");
    }

    public sealed record Message(int Value);

    public sealed class Handler : IOutboxHandler<Message>
    {
        private readonly ILogger<Handler> _logger;
        public Handler(ILogger<Handler> logger)
        {
            _logger = logger;
        }

        private static int _callCount;
        public static int CallCount => Volatile.Read(ref _callCount);

        public static void Reset()
        {
            Interlocked.Exchange(ref _callCount, 0);
        }

        public Task HandleAsync(IEnumerable<OutboxMessage<Message>> messages, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Handling messages.");
            foreach (var _ in messages)
                Interlocked.Increment(ref _callCount);

            return Task.CompletedTask;
        }
    }
}
