using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using Yautbox.Handlers;
using OutboxMessage = Yautbox.Testing.OutboxMessage;

namespace Yautbox.Testing.UnitTests;

public class OutboxMessageTests
{
    [Fact]
    public void Of_ShouldCarryPayloadAndDefaults()
    {
        // Arrange
        var before = DateTimeOffset.UtcNow;

        // Act
        var message = OutboxMessage.Of(new OrderPlaced("A-123"));

        // Assert
        message.Payload.Should().Be(new OrderPlaced("A-123"));
        message.Attempt.Should().Be(0);
        message.CreatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(DateTimeOffset.UtcNow);
    }

    [Fact]
    public void Of_ShouldUseGivenAttemptAndCreatedAt()
    {
        // Arrange
        var createdAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        // Act
        var message = OutboxMessage.Of(new OrderPlaced("A-123"), attempt: 2, createdAt: createdAt);

        // Assert
        message.Attempt.Should().Be(2);
        message.CreatedAt.Should().Be(createdAt);
    }

    [Fact]
    public void Retry_ShouldNotThrow()
    {
        // Arrange
        var message = OutboxMessage.Of(new OrderPlaced("A-123"));

        // Act
        var act = () =>
        {
            message.Retry(TimeSpan.FromSeconds(5));
            message.Retry(DateTimeOffset.UtcNow);
        };

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public async Task Of_ShouldBeAcceptedByHandler()
    {
        // Arrange
        var handler = new RecordingHandler();

        // Act
        await handler.HandleAsync([OutboxMessage.Of(new OrderPlaced("A")), OutboxMessage.Of(new OrderPlaced("B"))], CancellationToken.None);

        // Assert
        handler.OrderIds.Should().Equal("A", "B");
    }

    public sealed record OrderPlaced(string OrderId);

    private sealed class RecordingHandler : IOutboxHandler<OrderPlaced>
    {
        public List<string> OrderIds { get; } = [];

        public Task HandleAsync(IEnumerable<OutboxMessage<OrderPlaced>> messages, CancellationToken cancellationToken)
        {
            OrderIds.AddRange(messages.Select(m => m.Payload.OrderId));
            return Task.CompletedTask;
        }
    }
}
