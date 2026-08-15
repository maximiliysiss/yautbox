using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using Yautbox.Entities;
using Yautbox.InMemory.Infrastructure;
using Yautbox.InMemory.Options;
using Yautbox.InMemory.Provider;
using Yautbox.InMemory.UnitTests.Extensions;
using Yautbox.Provider.Contracts;
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
            identifier: typeof(Message).GetVersionFreeFullName(),
            count: 10,
            visibility: TimeSpan.FromSeconds(1),
            cancellationToken: CancellationToken.None);

        // Assert
        batch.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAsync_ShouldReturnEmpty_WhenTypeWasNotEnqueued()
    {
        // Arrange
        var provider = Create();
        var outboxMessage = CreateMessage();

        await provider.AddAsync(
            identifier: typeof(Message).GetVersionFreeFullName(),
            messages: [outboxMessage],
            cancellationToken: CancellationToken.None);

        // Act
        var batch = await provider.GetAsync<OtherMessage>(
            identifier: typeof(OtherMessage).GetVersionFreeFullName(),
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
            identifier: typeof(Message).GetVersionFreeFullName(),
            messages: [outboxMessage],
            cancellationToken: CancellationToken.None);

        var batch = await provider.GetAsync<Message>(
            identifier: typeof(Message).GetVersionFreeFullName(),
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
    public async Task GetAsync_ShouldReturnOnlyMessagesForRequestedType()
    {
        // Arrange
        var provider = Create();
        var firstMessage = CreateMessage();
        var secondMessage = CreateMessage();
        var otherMessage = CreateOtherMessage();

        await provider.AddAsync(
            identifier: typeof(Message).GetVersionFreeFullName(),
            messages: [firstMessage, secondMessage],
            cancellationToken: CancellationToken.None);

        await provider.AddAsync(
            identifier: typeof(OtherMessage).GetVersionFreeFullName(),
            messages: [otherMessage],
            cancellationToken: CancellationToken.None);

        // Act
        var batch = await provider.GetAsync<Message>(
            identifier: typeof(Message).GetVersionFreeFullName(),
            count: 10,
            visibility: TimeSpan.FromSeconds(1),
            cancellationToken: CancellationToken.None);

        // Assert
        batch.Should().HaveCount(2);
        batch.Select(message => message.Payload.Value).Should().BeEquivalentTo([firstMessage.Payload.Value, secondMessage.Payload.Value]);
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
            identifier: typeof(Message).GetVersionFreeFullName(),
            messages: outboxMessages,
            cancellationToken: CancellationToken.None);

        var firstBatch = await provider.GetAsync<Message>(
            identifier: typeof(Message).GetVersionFreeFullName(),
            count: 10,
            visibility: TimeSpan.FromSeconds(1),
            cancellationToken: CancellationToken.None);

        var secondBatch = await provider.GetAsync<Message>(
            identifier: typeof(Message).GetVersionFreeFullName(),
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
            identifier: typeof(Message).GetVersionFreeFullName(),
            messages: [outboxMessage],
            cancellationToken: CancellationToken.None);

        var firstBatch = await provider.GetAsync<Message>(
            identifier: typeof(Message).GetVersionFreeFullName(),
            count: 10,
            visibility: TimeSpan.FromSeconds(1),
            cancellationToken: CancellationToken.None);

        await Task.Delay(TimeSpan.FromSeconds(2));

        var secondBatch = await provider.GetAsync<Message>(
            identifier: typeof(Message).GetVersionFreeFullName(),
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
            identifier: typeof(Message).GetVersionFreeFullName(),
            messages: [outboxMessage],
            cancellationToken: CancellationToken.None);

        var firstBatch = await provider.GetAsync<Message>(
            identifier: typeof(Message).GetVersionFreeFullName(),
            count: 10,
            visibility: TimeSpan.FromSeconds(1),
            cancellationToken: CancellationToken.None);

        await Task.Delay(TimeSpan.FromSeconds(2));

        var secondBatch = await provider.GetAsync<Message>(
            identifier: typeof(Message).GetVersionFreeFullName(),
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
            identifier: typeof(Message).GetVersionFreeFullName(),
            messages: [outboxMessage],
            cancellationToken: CancellationToken.None);

        var firstBatch = await provider.GetAsync<Message>(
            identifier: typeof(Message).GetVersionFreeFullName(),
            count: 10,
            visibility: TimeSpan.FromSeconds(1),
            cancellationToken: CancellationToken.None);

        await provider.DeleteAsync(
            identifier: string.Empty,
            ids: firstBatch.Select(c => c.Id).ToArray(),
            policy: DeletePolicy.Delete,
            cancellationToken: CancellationToken.None);

        await Task.Delay(TimeSpan.FromSeconds(2));

        var secondBatch = await provider.GetAsync<Message>(
            identifier: typeof(Message).GetVersionFreeFullName(),
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
            identifier: typeof(Message).GetVersionFreeFullName(),
            messages: [outboxMessage],
            cancellationToken: CancellationToken.None);

        await provider.CancelAsync(
            identifier: string.Empty,
            ids: ids,
            policy: DeletePolicy.Safe,
            cancellationToken: CancellationToken.None);

        var firstBatch = await provider.GetAsync<Message>(
            identifier: typeof(Message).GetVersionFreeFullName(),
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
            identifier: typeof(Message).GetVersionFreeFullName(),
            messages: [outboxMessage],
            cancellationToken: CancellationToken.None);

        var firstBatch = await provider.GetAsync<Message>(
            identifier: typeof(Message).GetVersionFreeFullName(),
            count: 10,
            visibility: TimeSpan.FromSeconds(1),
            cancellationToken: CancellationToken.None);

        var getMessage = firstBatch.First();
        var retriedMessage = getMessage with { Attempt = getMessage.Attempt + 1 };

        await provider.RetryAsync(
            identifier: typeof(Message).GetVersionFreeFullName(),
            [retriedMessage],
            cancellationToken: CancellationToken.None);

        await Task.Delay(TimeSpan.FromSeconds(2));

        var secondBatch = await provider.GetAsync<Message>(
            identifier: typeof(Message).GetVersionFreeFullName(),
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

    [Fact]
    public async Task AddAsync_ShouldReturnEmpty_WhenNoMessagesProvided()
    {
        // Arrange
        var provider = Create();

        // Act
        var ids = await provider.AddAsync<Message>(
            identifier: typeof(Message).GetVersionFreeFullName(),
            messages: [],
            cancellationToken: CancellationToken.None);

        // Assert
        ids.Should().BeEmpty();
    }

    [Fact]
    public async Task AddAsync_ShouldEnqueueOnlyAfterTransactionComplete()
    {
        // Arrange
        var provider = Create();
        var message = CreateMessage();
        IReadOnlyCollection<OutboxMessageId> ids;

        // Act
        using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
        {
            ids = await provider.AddAsync(
                identifier: typeof(Message).GetVersionFreeFullName(),
                messages: [message],
                cancellationToken: CancellationToken.None);

            var batchBeforeCommit = await provider.GetAsync<Message>(
                identifier: typeof(Message).GetVersionFreeFullName(),
                count: 10,
                visibility: TimeSpan.FromSeconds(1),
                cancellationToken: CancellationToken.None);

            batchBeforeCommit.Should().BeEmpty();
            scope.Complete();
        }

        var batchAfterCommit = await WaitForBatchAsync(provider);

        // Assert
        ids.Should().ContainSingle().Which.Should().NotBe(OutboxMessageId.Empty);
        batchAfterCommit.Should().ContainSingle().Which.Should().BeEquivalentTo(message with { Id = ids.Single() });
    }

    [Fact]
    public async Task AddAsync_ShouldNotEnqueue_WhenTransactionNotCompleted()
    {
        // Arrange
        var provider = Create();
        var message = CreateMessage();

        // Act
        using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
        {
            await provider.AddAsync(
                identifier: typeof(Message).GetVersionFreeFullName(),
                messages: [message],
                cancellationToken: CancellationToken.None);
        }

        // Assert
        await WaitForNoBatchAsync(provider);
    }

    [Fact]
    public async Task AddAsync_ShouldPreserveProvidedId()
    {
        // Arrange
        var provider = Create();
        var messageId = new OutboxMessageId(42);
        var outboxMessage = CreateMessage() with { Id = messageId };

        // Act
        var ids = await provider.AddAsync(
            identifier: typeof(Message).GetVersionFreeFullName(),
            messages: [outboxMessage],
            cancellationToken: CancellationToken.None);

        var batch = await provider.GetAsync<Message>(
            identifier: typeof(Message).GetVersionFreeFullName(),
            count: 10,
            visibility: TimeSpan.FromSeconds(1),
            cancellationToken: CancellationToken.None);

        // Assert
        ids.Should().ContainSingle().Which.Should().Be(messageId);
        batch.Should().ContainSingle().Which.Should().Be(outboxMessage);
    }

    private static InMemoryOutboxProvider Create(InMemoryOutboxOptions? options = null, IDateTimeProvider? dateTimeProvider = null)
    {
        return new InMemoryOutboxProvider(
            options ?? new InMemoryOutboxOptions(),
            dateTimeProvider ?? new DateTimeProvider(),
            NullLogger<InMemoryOutboxProvider>.Instance);
    }

    private static async Task<IReadOnlyCollection<OutboxMessage<Message>>> WaitForBatchAsync(InMemoryOutboxProvider provider)
    {
        var timeout = TimeSpan.FromSeconds(2);
        var start = DateTimeOffset.UtcNow;

        while (DateTimeOffset.UtcNow - start < timeout)
        {
            var batch = await provider.GetAsync<Message>(
                identifier: typeof(Message).GetVersionFreeFullName(),
                count: 10,
                visibility: TimeSpan.FromSeconds(1),
                cancellationToken: CancellationToken.None);

            if (batch.Count > 0)
                return batch;

            await Task.Delay(TimeSpan.FromMilliseconds(25));
        }

        return [];
    }

    private static async Task WaitForNoBatchAsync(InMemoryOutboxProvider provider)
    {
        var timeout = TimeSpan.FromSeconds(1);
        var start = DateTimeOffset.UtcNow;

        while (DateTimeOffset.UtcNow - start < timeout)
        {
            var batch = await provider.GetAsync<Message>(
                identifier: typeof(Message).GetVersionFreeFullName(),
                count: 10,
                visibility: TimeSpan.FromSeconds(1),
                cancellationToken: CancellationToken.None);

            batch.Should().BeEmpty();
            await Task.Delay(TimeSpan.FromMilliseconds(25));
        }
    }

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

    private static OutboxMessage<OtherMessage> CreateOtherMessage(
        DateTimeOffset? createdAt = null,
        DateTimeOffset? scheduledAt = null)
    {
        return new OutboxMessage<OtherMessage>(
            Id: OutboxMessageId.Empty,
            Payload: new OtherMessage(Environment.TickCount),
            CreatedAt: createdAt ?? DateTimeOffset.UtcNow,
            Attempt: 0,
            ScheduledAt: scheduledAt);
    }

    private sealed record Message(int Value);

    private sealed record OtherMessage(int Value);
}

internal static class InMemoryOutboxProviderTestExtensions
{
    public static Task<IReadOnlyCollection<OutboxMessageId>> AddAsync<T>(
        this InMemoryOutboxProvider provider,
        string identifier,
        IReadOnlyCollection<OutboxMessage<T>> messages,
        CancellationToken cancellationToken) =>
        provider.AddAsync(
            messages.Select(message => new AddRequest<T>(identifier, message)).ToArray(),
            cancellationToken);
}
