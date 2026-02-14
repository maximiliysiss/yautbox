using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Yautbox.Extensions.Outbox;
using Yautbox.Handlers;
using Yautbox.Postgres.IntegrationTests.Shared.Extensions;
using Yautbox.Postgres.IntegrationTests.Shared.Fixture;
using Yautbox.Services;

namespace Yautbox.Postgres.IntegrationTests.Cases;

[Collection(nameof(IntegrationTestCollection))]
public class CancelledOutboxHandlerTests(IntegrationTestFixture fixture)
{
    [Fact]
    public async Task Handler_ShouldNotHandleCancelledMessage()
    {
        // Arrange
        Handler.Reset();
        using var scope = fixture.Services.CreateScope();

        var service = scope.ServiceProvider.GetRequiredService<IOutboxService>();
        var scheduledAt = DateTimeOffset.UtcNow.AddMilliseconds(300);
        var message = new Message(11);

        // Act
        var id = await service.HandleAsync(message: message, scheduledAt: scheduledAt);
        await service.CancelAsync<Message>(id, CancellationToken.None);

        await Task.Delay(TimeSpan.FromMilliseconds(700));

        // Assert
        Handler.CallCount.Should().Be(0);
    }

    public sealed record Message(int Value);

    public sealed class Handler : IOutboxHandler<Message>
    {
        private static int _callCount;
        public static int CallCount => Volatile.Read(ref _callCount);

        public static void Reset() => Interlocked.Exchange(ref _callCount, 0);

        public Task HandleAsync(IEnumerable<OutboxMessage<Message>> messages, CancellationToken cancellationToken)
        {
            foreach (var _ in messages)
                Interlocked.Increment(ref _callCount);

            return Task.CompletedTask;
        }
    }
}
