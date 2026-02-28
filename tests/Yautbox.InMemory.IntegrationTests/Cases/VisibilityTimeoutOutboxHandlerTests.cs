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
public class VisibilityTimeoutOutboxHandlerTests(IntegrationTestFixture fixture)
{
    [Fact]
    public async Task Handler_ShouldNotDuplicate_WhenHandlingExceedsVisibility()
    {
        // Arrange
        Handler.Reset();
        using var scope = fixture.Services.CreateScope();

        var service = scope.ServiceProvider.GetRequiredService<IOutboxService>();

        // Act
        await service.HandleAsync(message: new Message(61));

        await WaitForHandledAsync(TimeSpan.FromSeconds(2));
        await Task.Delay(TimeSpan.FromMilliseconds(400));

        // Assert
        Handler.CallCount.Should().Be(2);
    }

    private static async Task WaitForHandledAsync(TimeSpan timeout)
    {
        var stopwatch = Stopwatch.StartNew();
        while (stopwatch.Elapsed < timeout)
        {
            if (Handler.CallCount > 0)
                return;

            await Task.Delay(TimeSpan.FromMilliseconds(20));
        }

        throw new TimeoutException("Expected handler to process a message.");
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

        public async Task HandleAsync(IEnumerable<OutboxMessage<Message>> messages, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Handling messages.");
            foreach (var _ in messages)
                Interlocked.Increment(ref _callCount);

            await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);
        }
    }
}
