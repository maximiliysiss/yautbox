using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Yautbox.Extensions.Outbox;
using Yautbox.Handlers;
using Yautbox.Postgres.IntegrationTests.Shared.Fixture;
using Yautbox.Services;

namespace Yautbox.Postgres.IntegrationTests.Cases;

[Collection(nameof(IntegrationTestCollection))]
public class ScheduledOutboxHandlerTests(IntegrationTestFixture fixture)
{
    [Fact]
    public async Task Handler_ShouldHandleMessageOnlyAfterScheduledAt()
    {
        // Arrange
        Handler.Reset();
        using var scope = fixture.Services.CreateScope();

        var service = scope.ServiceProvider.GetRequiredService<IOutboxService>();
        var scheduledAt = DateTimeOffset.UtcNow.AddMilliseconds(300);
        var message = new Message(5);

        // Act
        await service.HandleAsync(message: message, scheduledAt: scheduledAt);

        await Task.Delay(TimeSpan.FromMilliseconds(150));

        // Assert
        Handler.CallCount.Should().Be(0);

        var handledAt = await WaitForHandledAtAsync(TimeSpan.FromSeconds(2));
        handledAt.Should().NotBeNull();
        handledAt!.Value.Should().BeOnOrAfter(scheduledAt.Subtract(TimeSpan.FromMilliseconds(50)));
    }

    private static async Task<DateTimeOffset?> WaitForHandledAtAsync(TimeSpan timeout)
    {
        var stopwatch = Stopwatch.StartNew();
        while (stopwatch.Elapsed < timeout)
        {
            var handledAt = Handler.HandledAt;
            if (handledAt.HasValue)
                return handledAt;

            await Task.Delay(TimeSpan.FromMilliseconds(20));
        }

        return null;
    }

    public sealed record Message(int Value);

    public sealed class Handler : IOutboxHandler<Message>
    {
        private static int _callCount;
        private static long _handledAtUtcTicks;

        public static int CallCount => Volatile.Read(ref _callCount);

        public static DateTimeOffset? HandledAt
        {
            get
            {
                var ticks = Volatile.Read(ref _handledAtUtcTicks);
                return ticks == 0 ? null : new DateTimeOffset(ticks, TimeSpan.Zero);
            }
        }

        public static void Reset()
        {
            Interlocked.Exchange(ref _callCount, 0);
            Interlocked.Exchange(ref _handledAtUtcTicks, 0);
        }

        public Task HandleAsync(IEnumerable<OutboxMessage<Message>> messages, CancellationToken cancellationToken)
        {
            foreach (var _ in messages)
                Interlocked.Increment(ref _callCount);

            Interlocked.Exchange(ref _handledAtUtcTicks, DateTimeOffset.UtcNow.UtcTicks);
            return Task.CompletedTask;
        }
    }
}
