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
using Yautbox.Postgres.IntegrationTests.Shared.Extensions;
using Yautbox.Postgres.IntegrationTests.Shared.Fixture;
using Yautbox.Services;

namespace Yautbox.Postgres.IntegrationTests.Cases;

[Collection(nameof(IntegrationTestCollection))]
public class HandleTimeoutOutboxHandlerTests(IntegrationTestFixture fixture)
{
    [Fact]
    public async Task Handler_ShouldRetry_WhenHandleTimeoutExceeded()
    {
        // Arrange
        Handler.Reset();
        using var scope = fixture.Services.CreateScope();

        var service = scope.ServiceProvider.GetRequiredService<IOutboxService>();

        // Act
        await service.HandleAsync(message: new Message(71));

        var successAttempt = await WaitForSuccessAttemptAsync(TimeSpan.FromSeconds(3));

        // Assert
        successAttempt.Should().Be(2);
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
        private static int _callCount;
        private static int _succeeded;
        private static int _successAttempt = -1;

        public static int CallCount => Volatile.Read(ref _callCount);
        public static int SuccessAttempt => Volatile.Read(ref _successAttempt);

        public static void Reset()
        {
            Interlocked.Exchange(ref _callCount, 0);
            Interlocked.Exchange(ref _succeeded, 0);
            Interlocked.Exchange(ref _successAttempt, -1);
        }

        public async Task HandleAsync(IEnumerable<OutboxMessage<Message>> messages, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _callCount);
            var message = messages.First();

            if (Interlocked.Exchange(ref _succeeded, 1) == 0)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);
                return;
            }

            Volatile.Write(ref _successAttempt, message.Attempt + 1);
        }
    }
}
