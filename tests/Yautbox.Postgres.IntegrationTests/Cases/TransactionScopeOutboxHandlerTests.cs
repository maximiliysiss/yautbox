using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Yautbox.Extensions.Outbox;
using Yautbox.Handlers;
using Yautbox.Postgres.IntegrationTests.Shared.Fixture;
using Yautbox.Services;

namespace Yautbox.Postgres.IntegrationTests.Cases;

[Collection(nameof(IntegrationTestCollection))]
public class TransactionScopeOutboxHandlerTests(IntegrationTestFixture fixture)
{
    [Fact]
    public async Task Handler_ShouldEnqueueAfterTransactionCommitted()
    {
        // Arrange
        Handler.Reset();
        using var scope = fixture.Services.CreateScope();

        var service = scope.ServiceProvider.GetRequiredService<IOutboxService>();

        // Act
        using (var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
        {
            await service.HandleAsync(message: new Message(51));

            await Task.Delay(TimeSpan.FromMilliseconds(200));
            Handler.CallCount.Should().Be(0);

            transaction.Complete();
        }

        await WaitForHandledAsync(TimeSpan.FromSeconds(2));

        // Assert
        Handler.CallCount.Should().Be(1);
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

        throw new TimeoutException("Expected handler to process a message after commit.");
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
