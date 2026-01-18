using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using Yautbox.Entities;
using Yautbox.InMemory.Infrastructure;
using Yautbox.InMemory.Options;
using Yautbox.InMemory.Provider;
using Yautbox.Runner.Options;

namespace Yautbox.InMemory.UnitTests.Provider;

public class InMemoryOutboxProviderTests
{
    [Fact]
    public async Task GetAsync_ShouldReturnEmpty_WhenQueueIsEmpty()
    {
        // Arrange
        var provider = Create();

        // Act
        var batch = await provider.GetAsync<Message>(
            count: 10,
            visibility: TimeSpan.FromSeconds(1),
            cancellationToken: CancellationToken.None);

        // Assert
        batch.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAsync_ShouldReturnRecord_WhenItWasAdded()
    {
        // Arrange
        var provider = Create();
        var outboxMessage = CreateMessage();

        // Act
        var ids = await provider.AddAsync(
            messages: [outboxMessage],
            cancellationToken: CancellationToken.None);

        var batch = await provider.GetAsync<Message>(
            count: 10,
            visibility: TimeSpan.FromSeconds(1),
            cancellationToken: CancellationToken.None);

        // Assert
        ids
            .Should().ContainSingle()
            .Which.Should().NotBe(OutboxMessageId.Empty);

        batch
            .Should().ContainSingle()
            .Which.Should().BeEquivalentTo(outboxMessage with { Id = ids.First() });
    }

    [Fact]
    public async Task GetAsync_ShouldReturnBatch_WhenGetByPage()
    {
        // Arrange
        var provider = Create();

        var outboxMessages = Enumerable
            .Range(0, 15)
            .Select(_ => CreateMessage())
            .ToArray();

        // Act
        var ids = await provider.AddAsync(
            messages: outboxMessages,
            cancellationToken: CancellationToken.None);

        var firstBatch = await provider.GetAsync<Message>(
            count: 10,
            visibility: TimeSpan.FromSeconds(1),
            cancellationToken: CancellationToken.None);

        var secondBatch = await provider.GetAsync<Message>(
            count: 10,
            visibility: TimeSpan.FromSeconds(1),
            cancellationToken: CancellationToken.None);

        // Assert
        ids.Should().HaveCount(15);

        firstBatch.Should().HaveCount(10);
        secondBatch.Should().HaveCount(5);

        firstBatch.Select(c => c.Id)
            .Concat(secondBatch.Select(c => c.Id))
            .Distinct()
            .Should().HaveCount(15);
    }

    [Fact]
    public async Task GetAsync_ShouldReturnRecord_WhenRecordWasScheduled()
    {
        // Arrange
        var provider = Create();

        var scheduledAt = DateTimeOffset.UtcNow.AddSeconds(1);
        var outboxMessage = CreateMessage(scheduledAt: scheduledAt);

        // Act
        var ids = await provider.AddAsync(
            messages: [outboxMessage],
            cancellationToken: CancellationToken.None);

        var firstBatch = await provider.GetAsync<Message>(
            count: 10,
            visibility: TimeSpan.FromSeconds(1),
            cancellationToken: CancellationToken.None);

        await Task.Delay(TimeSpan.FromSeconds(2));

        var secondBatch = await provider.GetAsync<Message>(
            count: 10,
            visibility: TimeSpan.FromSeconds(1),
            cancellationToken: CancellationToken.None);

        // Assert
        firstBatch.Should().BeEmpty();

        secondBatch
            .Should().ContainSingle()
            .Which.Should().Be(outboxMessage with { Id = ids.First() });
    }

    [Fact]
    public async Task GetAsync_ShouldReturnRecordAgain_WhenVisibilityExpired()
    {
        // Arrange
        var provider = Create();
        var outboxMessage = CreateMessage();

        // Act
        var ids = await provider.AddAsync(
            messages: [outboxMessage],
            cancellationToken: CancellationToken.None);

        var firstBatch = await provider.GetAsync<Message>(
            count: 10,
            visibility: TimeSpan.FromSeconds(1),
            cancellationToken: CancellationToken.None);

        await Task.Delay(TimeSpan.FromSeconds(2));

        var secondBatch = await provider.GetAsync<Message>(
            count: 10,
            visibility: TimeSpan.FromSeconds(1),
            cancellationToken: CancellationToken.None);

        // Assert
        ids
            .Should().ContainSingle()
            .Which.Should().NotBe(OutboxMessageId.Empty);

        firstBatch
            .Should().ContainSingle()
            .Which.Should().BeEquivalentTo(outboxMessage with { Id = ids.First() });

        secondBatch
            .Should().ContainSingle()
            .Which.Should().BeEquivalentTo(outboxMessage with { Id = ids.First() });
    }

    [Fact]
    public async Task GetAsync_ShouldReturnRecordOnce_WhenRecordWasDelete()
    {
        // Arrange
        var provider = Create();
        var outboxMessage = CreateMessage();

        // Act
        var ids = await provider.AddAsync(
            messages: [outboxMessage],
            cancellationToken: CancellationToken.None);

        var firstBatch = await provider.GetAsync<Message>(
            count: 10,
            visibility: TimeSpan.FromSeconds(1),
            cancellationToken: CancellationToken.None);

        await provider.DeleteAsync(
            ids: firstBatch.Select(c => c.Id).ToArray(),
            policy: OutboxDeletePolicy.Delete,
            cancellationToken: CancellationToken.None);

        await Task.Delay(TimeSpan.FromSeconds(2));

        var secondBatch = await provider.GetAsync<Message>(
            count: 10,
            visibility: TimeSpan.FromSeconds(1),
            cancellationToken: CancellationToken.None);

        // Assert
        ids
            .Should().ContainSingle()
            .Which.Should().NotBe(OutboxMessageId.Empty);

        firstBatch
            .Should().ContainSingle()
            .Which.Should().BeEquivalentTo(outboxMessage with { Id = ids.First() });

        secondBatch
            .Should().BeEmpty();
    }

    [Fact]
    public async Task GetAsync_ShouldReturnEmpty_WhenRecordWasCancelled()
    {
        // Arrange
        var provider = Create();
        var outboxMessage = CreateMessage();

        // Act
        var ids = await provider.AddAsync(
            messages: [outboxMessage],
            cancellationToken: CancellationToken.None);

        await provider.CancelAsync(ids, CancellationToken.None);

        var firstBatch = await provider.GetAsync<Message>(
            count: 10,
            visibility: TimeSpan.FromSeconds(1),
            cancellationToken: CancellationToken.None);

        // Assert
        ids
            .Should().ContainSingle()
            .Which.Should().NotBe(OutboxMessageId.Empty);

        firstBatch.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAsync_ShouldReturnRecordWithoutDuplicates_WhenThereIsRetries()
    {
        // Arrange
        var provider = Create();
        var outboxMessage = CreateMessage();

        // Act
        var ids = await provider.AddAsync(
            messages: [outboxMessage],
            cancellationToken: CancellationToken.None);

        var firstBatch = await provider.GetAsync<Message>(
            count: 10,
            visibility: TimeSpan.FromSeconds(1),
            cancellationToken: CancellationToken.None);

        var getMessage = firstBatch.First();
        var retriedMessage = getMessage with { Attempt = getMessage.Attempt + 1 };

        await provider.RetryAsync(
            [retriedMessage],
            cancellationToken: CancellationToken.None);

        await Task.Delay(TimeSpan.FromSeconds(2));

        var secondBatch = await provider.GetAsync<Message>(
            count: 10,
            visibility: TimeSpan.FromSeconds(1),
            cancellationToken: CancellationToken.None);

        // Assert
        ids
            .Should().ContainSingle()
            .Which.Should().NotBe(OutboxMessageId.Empty);

        firstBatch
            .Should().ContainSingle()
            .Which.Should().BeEquivalentTo(getMessage);

        secondBatch
            .Should().ContainSingle()
            .Which.Should().BeEquivalentTo(retriedMessage);
    }

    private static InMemoryOutboxProvider Create(InMemoryOutboxOptions? options = null, IDateTimeProvider? dateTimeProvider = null)
        => new(options ?? new InMemoryOutboxOptions(), dateTimeProvider ?? new DateTimeProvider());

    private static OutboxMessage<Message> CreateMessage(
        DateTimeOffset? createdAt = null,
        DateTimeOffset? scheduledAt = null)
    {
        return new OutboxMessage<Message>(
            Id: OutboxMessageId.Empty,
            Payload: new Message(Environment.TickCount),
            CreatedAt: createdAt ?? DateTimeOffset.UtcNow,
            Attempt: 0,
            ScheduledAt: scheduledAt);
    }

    private sealed record Message(int Value);
}
