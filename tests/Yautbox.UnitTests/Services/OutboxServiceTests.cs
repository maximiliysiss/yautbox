using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;
using Yautbox.Entities;
using Yautbox.Extensions.Outbox;
using Yautbox.Infrastructure.DateTime;
using Yautbox.Infrastructure.Waiter;
using Yautbox.Provider;
using Yautbox.Services;

namespace Yautbox.UnitTests.Services;

public class OutboxServiceTests
{
    [Fact]
    public async Task HandleAsync_ShouldHandleSuccess()
    {
        // Arrange
        var outboxMessages = new List<OutboxMessage<Message>>();

        var outboxProvider = Substitute.For<IOutboxProvider>();
        outboxProvider
            .AddAsync(Arg.Any<IReadOnlyCollection<OutboxMessage<Message>>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyCollection<OutboxMessageId>>([OutboxMessageId.Empty]))
            .AndDoes(c => outboxMessages.AddRange(c.Arg<IReadOnlyCollection<OutboxMessage<Message>>>()));

        var now = DateTimeOffset.UtcNow;

        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider
            .GetNow()
            .Returns(now);

        var service = Create(outboxProvider, dateTimeProvider: dateTimeProvider);

        var message = new Message();

        // Act
        var outboxMessageId = await service.HandleAsync(message: message);

        // Assert
        outboxMessageId.Should().Be(OutboxMessageId.Empty);

        var expected = new OutboxMessage<Message>(
            Id: OutboxMessageId.Empty,
            Payload: message,
            CreatedAt: now,
            Attempt: 0,
            ScheduledAt: null);

        outboxMessages
            .Should().ContainSingle()
            .Which.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task CancelAsync_ShouldCallCancel()
    {
        // Arrange
        var ids = new List<OutboxMessageId>();

        var outboxProvider = Substitute.For<IOutboxProvider>();
        outboxProvider
            .CancelAsync(Arg.Any<IReadOnlyCollection<OutboxMessageId>>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask)
            .AndDoes(c => ids.AddRange(c.Arg<IReadOnlyCollection<OutboxMessageId>>()));

        var service = Create(outboxProvider);

        // Act
        await service.CancelAsync(id: OutboxMessageId.Empty, CancellationToken.None);

        // Assert
        ids
            .Should().ContainSingle()
            .Which.Should().BeEquivalentTo(OutboxMessageId.Empty);
    }

    private static OutboxService Create(
        IOutboxProvider? outboxProvider = null,
        IInfrastructureReadinessWaiter? waiter = null,
        IDateTimeProvider? dateTimeProvider = null)
    {
        if (waiter is null)
        {
            waiter = Substitute.For<IInfrastructureReadinessWaiter>();

            waiter
                .WaitAsync(CancellationToken.None)
                .Returns(Task.CompletedTask);
        }

        return new OutboxService(
            outboxProvider ?? Substitute.For<IOutboxProvider>(),
            NullLogger<OutboxService>.Instance,
            waiter,
            dateTimeProvider ?? new DateTimeProvider());
    }

    private sealed class Message;
}
