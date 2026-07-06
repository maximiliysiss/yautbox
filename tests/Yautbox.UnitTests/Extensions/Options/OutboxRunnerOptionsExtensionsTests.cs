using System;
using FluentAssertions;
using Xunit;
using Yautbox.Extensions.Options;
using Yautbox.Runner.Options;

namespace Yautbox.UnitTests.Extensions.Options;

public class OutboxRunnerOptionsExtensionsTests
{
    [Fact]
    public void Validate_ShouldFail_WhenRetryCountIsNegative()
    {
        // Arrange
        var options = new TestRunnerOptions { RetryCount = -1 };

        // Act
        var result = options.Validate();

        // Assert
        result.Should().BeOfType<ValidationResult.FailureValidationResult>()
            .Which.ErrorMessage.Should().Be("Retry count cannot be negative");
    }

    [Fact]
    public void Validate_ShouldFail_WhenRetryDelayIsNegative()
    {
        // Arrange
        var options = new TestRunnerOptions { RetryDelays = [TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(-1)] };

        // Act
        var result = options.Validate();

        // Assert
        result.Should().BeOfType<ValidationResult.FailureValidationResult>()
            .Which.ErrorMessage.Should().Be("Retry delays cannot be negative");
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
        public TimeSpan FailureDelay => TimeSpan.FromSeconds(1);
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
