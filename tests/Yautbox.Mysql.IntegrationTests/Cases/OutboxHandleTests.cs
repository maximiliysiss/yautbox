using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Xunit;
using Yautbox.Handlers;
using Yautbox.Mysql.IntegrationTests.Shared.Extensions;
using Yautbox.Mysql.IntegrationTests.Shared.Fixture;
using Yautbox.Services;

namespace Yautbox.Mysql.IntegrationTests.Cases;

[Collection(nameof(IntegrationTestCollection))]
public class OutboxHandleTests(IntegrationTestFixture fixture)
{
    [Theory, AutoData]
    public async Task Handle_ShouldSendMessageAndHandleAsync(OutboxHandleTestsEvent message)
    {
        // Arrange
        using var serviceScope = fixture.Services.CreateScope();

        var outboxService = serviceScope.ServiceProvider.GetRequiredService<IOutboxService>();

        // Act
        await outboxService.HandleAsync(
            message: message,
            cancellationToken: CancellationToken.None);

        // Assert
        var counter = Polly.Policy
            .HandleResult<int>(i => i is 0)
            .WaitAndRetry(3, a => TimeSpan.FromSeconds(Math.Min(10, a * a)))
            .Execute(() => OutboxHandleTestsHandler.Counter);

        counter.Should().BeGreaterThan(0);
    }

    public sealed class OutboxHandleTestsHandler : IOutboxHandler<OutboxHandleTestsEvent>
    {
        private static int _counter;
        public static int Counter => _counter;

        public Task HandleAsync(IEnumerable<OutboxMessage<OutboxHandleTestsEvent>> messages, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _counter);
            return Task.CompletedTask;
        }
    }

    public sealed record OutboxHandleTestsEvent(int Id, string Name);
}
