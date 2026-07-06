using System;
using FluentAssertions;
using Xunit;
using Yautbox.Entities;
using Yautbox.Infrastructure.DateTime;
using Yautbox.Runner.Infrastructure;
using Yautbox.Runner.Options;

namespace Yautbox.UnitTests.Runner.Infrastructure;

public class OutboxRunnerContextTests
{
    [Fact]
    public void AddRetry_ShouldMarkMessageAsRetryExceeded_WhenRetryCountIsExceeded()
    {
        // Arrange
        var message = CreateMessage(attempt: 1);
        var options = new TestRunnerOptions { RetryCount = 1 };
        var context = new OutboxRunnerContext<Message>(new TestDateTimeProvider(), [message], options);
        var handlerMessage = new Handlers.OutboxMessage<Message>(message, context);

        // Act
        handlerMessage.Retry((DateTimeOffset?)null);

        // Assert
        context.Retries.Should().BeEmpty();
        context.RetryExceeded.Should().ContainSingle().Which.Should().Be(message.Id);
        context.Success.Should().BeEmpty();
    }

    [Fact]
    public void Fail_ShouldUseRetryDelayForCurrentAttempt()
    {
        // Arrange
        var now = new DateTimeOffset(2026, 7, 6, 12, 0, 0, TimeSpan.Zero);
        var message = CreateMessage(attempt: 1);
        var options = new TestRunnerOptions
        {
            FailureDelay = TimeSpan.FromMinutes(1),
            RetryDelays = [TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(30)]
        };

        var context = new OutboxRunnerContext<Message>(new TestDateTimeProvider(now), [message], options);

        // Act
        context.Fail();

        // Assert
        context.Retries
            .Should().ContainSingle()
            .Which.ScheduledAt.Should().Be(now.AddSeconds(30));
    }

    [Fact]
    public void Fail_ShouldReuseLastRetryDelay_WhenAttemptExceedsRetryDelays()
    {
        // Arrange
        var now = new DateTimeOffset(2026, 7, 6, 12, 0, 0, TimeSpan.Zero);
        var message = CreateMessage(attempt: 42);
        var options = new TestRunnerOptions
        {
            FailureDelay = TimeSpan.FromMinutes(1),
            RetryDelays = [TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(30)]
        };

        var context = new OutboxRunnerContext<Message>(new TestDateTimeProvider(now), [message], options);

        // Act
        context.Fail();

        // Assert
        context.Retries
            .Should().ContainSingle()
            .Which.ScheduledAt.Should().Be(now.AddSeconds(30));
    }

    private static OutboxMessage<Message> CreateMessage(int attempt)
        => new(
            Id: new OutboxMessageId(1),
            Payload: new Message(42),
            CreatedAt: DateTimeOffset.UtcNow,
            Attempt: attempt,
            ScheduledAt: null);

    private sealed record Message(int Value);

    private sealed class TestDateTimeProvider(DateTimeOffset? now = null) : IDateTimeProvider
    {
        public DateTimeOffset GetNow() => now ?? DateTimeOffset.UtcNow;
    }

    private sealed class TestRunnerOptions : IOutboxRunnerOptions
    {
        public string? Identifier => null;
        public TimeSpan PollDelay => TimeSpan.FromSeconds(1);
        public int BufferSize => 1;
        public TimeSpan HandleTimeout => TimeSpan.FromSeconds(1);
        public bool IsEnabled => true;
        public int WorkersCount => 1;
        public int PerBufferCount => 1;
        public DeletePolicy DeletePolicy => DeletePolicy.Safe;
        public TimeSpan FailureDelay { get; init; } = TimeSpan.FromSeconds(1);
        public int? RetryCount { get; init; }
        public TimeSpan[] RetryDelays { get; init; } = [];
        public TimeSpan Visibility => TimeSpan.FromSeconds(1);
        public TimeSpan? BackupInterval => null;
        public TimeSpan CleanupInterval => TimeSpan.FromSeconds(1);
        public ExecutionPolicy ExecutionPolicy => ExecutionPolicy.Parallel;
        public TimeSpan PolicyTimeout => TimeSpan.FromSeconds(1);
        public DeletePolicy CancellationPolicy => DeletePolicy.Safe;
        public ScopeLifetime ScopeLifetime => ScopeLifetime.PerBatch;
    }
}
