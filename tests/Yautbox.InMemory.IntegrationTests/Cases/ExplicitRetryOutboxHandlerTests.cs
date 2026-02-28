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
using Yautbox.InMemory.IntegrationTests.Shared.Extensions;
using Yautbox.InMemory.IntegrationTests.Shared.Fixture;
using Yautbox.Services;
using Microsoft.Extensions.Logging;
namespace Yautbox.InMemory.IntegrationTests.Cases;

[Collection(nameof(IntegrationTestCollection))]
public class ExplicitRetryOutboxHandlerTests(IntegrationTestFixture fixture)
{
    [Fact]
    public async Task Handler_ShouldRetryWhenExplicitlyRequested()
    {
        // Arrange
        Handler.Reset();
        using var scope = fixture.Services.CreateScope();

        var service = scope.ServiceProvider.GetRequiredService<IOutboxService>();
        var message = new Message(77);

        // Act
        await service.HandleAsync(message: message);

        var successAttempt = await WaitForSuccessAttemptAsync(TimeSpan.FromSeconds(2));

        // Assert
        successAttempt.Should().Be(1);
        Handler.CallCount.Should().BeGreaterThanOrEqualTo(2);
    }

    private static async Task<int?> WaitForSuccessAttemptAsync(TimeSpan timeout)
    {
        var stopwatch = Stopwatch.StartNew();
        while (stopwatch.Elapsed < timeout)
        {
            var attempt = Handler.SuccessAttempt;
            if (attempt >= 0)
                return attempt;

            await Task.Delay(TimeSpan.FromMilliseconds(20));
        }

        return null;
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
        private static int _requestedRetry;
        private static int _successAttempt = -1;

        public static int CallCount => Volatile.Read(ref _callCount);
        public static int SuccessAttempt => Volatile.Read(ref _successAttempt);

        public static void Reset()
        {
            Interlocked.Exchange(ref _callCount, 0);
            Interlocked.Exchange(ref _requestedRetry, 0);
            Interlocked.Exchange(ref _successAttempt, -1);
        }

        public Task HandleAsync(IEnumerable<OutboxMessage<Message>> messages, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Handling messages.");
            Interlocked.Increment(ref _callCount);
            var message = messages.First();

            if (Interlocked.Exchange(ref _requestedRetry, 1) == 0)
            {
                _logger.LogInformation("Invariant hit: if condition evaluated true.");
                message.Retry(TimeSpan.Zero);
                return Task.CompletedTask;
        }

            Volatile.Write(ref _successAttempt, message.Attempt);
            return Task.CompletedTask;
        }
    }
}
