using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Xunit;
using Yautbox.Handlers;
using Yautbox.Postgres.IntegrationTests.Shared.Extensions;
using Yautbox.Postgres.IntegrationTests.Shared.Fixture;
using Yautbox.Services;

namespace Yautbox.Postgres.IntegrationTests.Cases;

[Collection(nameof(IntegrationTestCollection))]
public class SimpleOutboxHandlerTests(IntegrationTestFixture fixture)
{
    [Fact]
    public async Task Handler_ShouldHandleNewRecord()
    {
        // Arrange
        using var scope = fixture.Services.CreateScope();

        var service = scope.ServiceProvider.GetRequiredService<IOutboxService>();

        var message = new Message(42);

        // Act
        await service.HandleAsync(message: message);

        // Assert
        var result = Polly.Policy
            .HandleResult<int>(a => a is 0)
            .WaitAndRetry(3, _ => TimeSpan.FromSeconds(1))
            .Execute(() => Handler.Index);

        result.Should().Be(message.Value);
    }

    public sealed record Message(int Value);

    public sealed class Handler : IOutboxHandler<Message>
    {
        private static int _index;
        public static int Index => _index;

        public Task HandleAsync(IEnumerable<OutboxMessage<Message>> messages, CancellationToken cancellationToken)
        {
            foreach (var message in messages)
                Volatile.Write(ref _index, message.Payload.Value);

            return Task.CompletedTask;
        }
    }
}
