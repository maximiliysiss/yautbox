using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
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
        var context = new OutboxRunnerContext<Message>(NullLogger.Instance, new TestDateTimeProvider(), [message], options);
        var handlerMessage = new Handlers.OutboxMessage<Message>(message, context);

        // Act
        handlerMessage.Retry((DateTimeOffset?)null);

        // Assert
        context.Retries.Should().BeEmpty();
        context.RetryExceeded.Should().ContainSingle().Which.Should().Be(message.Id);
        context.Success.Should().BeEmpty();
    }

    [Fact]
    public void AddRetry_ShouldLogWarning_WhenRetryCountIsExceeded()
    {
        // Arrange
        var logger = new CollectingLogger();
        var message = CreateMessage(attempt: 1);
        var options = new TestRunnerOptions { RetryCount = 1 };
        var context = new OutboxRunnerContext<Message>(logger, new TestDateTimeProvider(), [message], options);

        // Act
        context.Fail();

        // Assert
        logger.Warnings.Should().ContainSingle()
            .Which.Should().Contain("has exceeded retry count 1");
    }

    [Fact]
    public void AddRetry_ShouldNotLog_WhenRetryCountIsNotExceeded()
    {
        // Arrange
        var logger = new CollectingLogger();
        var message = CreateMessage(attempt: 0);
        var options = new TestRunnerOptions { RetryCount = 2 };
        var context = new OutboxRunnerContext<Message>(logger, new TestDateTimeProvider(), [message], options);

        // Act
        context.Fail();

        // Assert
        logger.Warnings.Should().BeEmpty();
        context.Retries.Should().ContainSingle();
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

        var context = new OutboxRunnerContext<Message>(NullLogger.Instance, new TestDateTimeProvider(now), [message], options);

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

        var context = new OutboxRunnerContext<Message>(NullLogger.Instance, new TestDateTimeProvider(now), [message], options);

        // Act
        context.Fail();

        // Assert
        context.Retries
            .Should().ContainSingle()
            .Which.ScheduledAt.Should().Be(now.AddSeconds(30));
    }

    [Fact]
    public void Fail_ShouldRetryInNextCycle_WhenSequentialAndNoRetryDelays()
    {
        // Arrange
        var message = CreateMessage(attempt: 0);
        var options = new TestRunnerOptions
        {
            ExecutionPolicy = ExecutionPolicy.Sequential,
            FailureDelay = TimeSpan.FromMinutes(1)
        };

        var context = new OutboxRunnerContext<Message>(NullLogger.Instance, new TestDateTimeProvider(), [message], options);

        // Act
        context.Fail();

        // Assert
        context.Retries
            .Should().ContainSingle()
            .Which.ScheduledAt.Should().BeNull();
    }

    [Fact]
    public void Fail_ShouldUseFailureDelay_WhenParallelAndNoRetryDelays()
    {
        // Arrange
        var now = new DateTimeOffset(2026, 7, 6, 12, 0, 0, TimeSpan.Zero);
        var message = CreateMessage(attempt: 0);
        var options = new TestRunnerOptions { FailureDelay = TimeSpan.FromMinutes(1) };

        var context = new OutboxRunnerContext<Message>(NullLogger.Instance, new TestDateTimeProvider(now), [message], options);

        // Act
        context.Fail();

        // Assert
        context.Retries
            .Should().ContainSingle()
            .Which.ScheduledAt.Should().Be(now.AddMinutes(1));
    }

    [Theory]
    [InlineData(ExecutionPolicy.Sequential, 1)]
    [InlineData(ExecutionPolicy.Parallel, 0)]
    public void Retry_WithDelay_ShouldWarnAboutReordering_OnlyWhenSequential(ExecutionPolicy policy, int expected)
    {
        // Arrange
        var logger = new CollectingLogger();
        var message = CreateMessage(attempt: 0);
        var options = new TestRunnerOptions { ExecutionPolicy = policy };
        var context = new OutboxRunnerContext<Message>(logger, new TestDateTimeProvider(), [message], options);
        var handlerMessage = new Handlers.OutboxMessage<Message>(message, context);

        // Act
        handlerMessage.Retry(TimeSpan.FromSeconds(30));

        // Assert
        logger.Warnings.Should().HaveCount(expected);
    }

    [Fact]
    public void Retry_WithoutDelay_ShouldNotWarnAboutReordering_WhenSequential()
    {
        // Arrange
        var logger = new CollectingLogger();
        var message = CreateMessage(attempt: 0);
        var options = new TestRunnerOptions { ExecutionPolicy = ExecutionPolicy.Sequential };
        var context = new OutboxRunnerContext<Message>(logger, new TestDateTimeProvider(), [message], options);
        var handlerMessage = new Handlers.OutboxMessage<Message>(message, context);

        // Act
        handlerMessage.Retry((TimeSpan?)null);

        // Assert
        logger.Warnings.Should().BeEmpty();
        context.Retries.Should().ContainSingle().Which.ScheduledAt.Should().BeNull();
    }

    [Fact]
    public void Retry_WithDelay_ShouldNotWarnAboutReordering_WhenRetryCountIsExceeded()
    {
        // Arrange
        var logger = new CollectingLogger();
        var message = CreateMessage(attempt: 1);
        var options = new TestRunnerOptions { ExecutionPolicy = ExecutionPolicy.Sequential, RetryCount = 1 };
        var context = new OutboxRunnerContext<Message>(logger, new TestDateTimeProvider(), [message], options);
        var handlerMessage = new Handlers.OutboxMessage<Message>(message, context);

        // Act
        handlerMessage.Retry(TimeSpan.FromSeconds(30));

        // Assert
        logger.Warnings.Should().ContainSingle("only the retry count warning is expected")
            .Which.Should().Contain("has exceeded retry count").And.NotContain("Sequential");
        context.RetryExceeded.Should().ContainSingle().Which.Should().Be(message.Id);
    }

    [Fact]
    public void Fail_ShouldNotWarnAboutReordering_WhenSequentialWithRetryDelays()
    {
        // Arrange
        var logger = new CollectingLogger();
        var message = CreateMessage(attempt: 0);
        var options = new TestRunnerOptions
        {
            ExecutionPolicy = ExecutionPolicy.Sequential,
            RetryDelays = [TimeSpan.FromSeconds(5)]
        };

        var context = new OutboxRunnerContext<Message>(logger, new TestDateTimeProvider(), [message], options);

        // Act
        context.Fail();

        // Assert
        logger.Warnings.Should().BeEmpty();
    }

    private static OutboxMessage<Message> CreateMessage(int attempt)
        => new(
            Id: new OutboxMessageId(1),
            Payload: new Message(42),
            CreatedAt: DateTimeOffset.UtcNow,
            Attempt: attempt,
            ScheduledAt: null);

    private sealed record Message(int Value);

    private sealed class CollectingLogger : ILogger
    {
        public List<string> Warnings { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (logLevel is LogLevel.Warning)
                Warnings.Add(formatter(state, exception));
        }
    }

    private sealed class TestDateTimeProvider(DateTimeOffset? now = null) : IDateTimeProvider
    {
        public DateTimeOffset GetNow() => now ?? DateTimeOffset.UtcNow;
    }

    private sealed class TestRunnerOptions : IOutboxRunnerOptions<Message>
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
        public ExecutionPolicy ExecutionPolicy { get; init; } = ExecutionPolicy.Parallel;
        public TimeSpan PolicyTimeout => TimeSpan.FromSeconds(1);
        public DeletePolicy CancellationPolicy => DeletePolicy.Safe;
        public ScopeLifetime ScopeLifetime => ScopeLifetime.PerBatch;
    }
}
