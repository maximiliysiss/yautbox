using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Yautbox.Handlers;
using Yautbox.Mysql.IntegrationTests.Shared.Extensions;
using Yautbox.Mysql.IntegrationTests.Shared.Fixture;
using Yautbox.Services;

namespace Yautbox.Mysql.IntegrationTests.Cases;

[Collection(nameof(IntegrationTestCollection))]
public class DisabledOutboxHandlerTests(IntegrationTestFixture fixture)
{
    [Fact]
    public async Task Handler_ShouldNotRun_WhenDisabled()
    {
        // Arrange
        Handler.Reset();
        using var scope = fixture.Services.CreateScope();

        var service = scope.ServiceProvider.GetRequiredService<IOutboxService>();

        // Act
        await service.HandleAsync(message: new Message(21));

        await Task.Delay(TimeSpan.FromMilliseconds(400));

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
