using System;
using System.Linq;
using FluentAssertions;
using Xunit;
using Yautbox.Runner.Options;
using Yautbox.Runner.Validation;

namespace Yautbox.UnitTests.Runner.Validation;

public class OutboxOptionsValidatorTests
{
    [Fact]
    public void Validate_ShouldReturnNothing_WhenOptionsAreValid()
    {
        // Act
        var results = Validate(new TestRunnerOptions());

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public void Validate_ShouldCollectAllFailures()
    {
        // Arrange
        var options = new TestRunnerOptions
        {
            PollDelay = TimeSpan.Zero,
            BufferSize = 0,
            RetryCount = -1
        };

        // Act
        var errors = Failures(options);

        // Assert
        errors.Should().Contain([
            "PollDelay must be greater than zero",
            "BufferSize must be greater than zero",
            "RetryCount must be greater than zero"
        ]);
    }

    [Theory]
    [MemberData(nameof(InvalidOptions))]
    public void Validate_ShouldFail_WhenOptionIsInvalid(TestRunnerOptions options, string expected)
    {
        // Act
        var errors = Failures(options);

        // Assert
        errors.Should().Contain(expected);
    }

    public static TheoryData<TestRunnerOptions, string> InvalidOptions => new()
    {
        { new TestRunnerOptions { PerBufferCount = 11, BufferSize = 10 }, "PerBufferCount must be less than or equal to BufferSize" },
        { new TestRunnerOptions { CleanupInterval = TimeSpan.Zero }, "CleanupInterval must be greater than zero" },
        { new TestRunnerOptions { PolicyTimeout = TimeSpan.Zero }, "PolicyTimeout must be greater than zero" },
        { new TestRunnerOptions { RetryCount = 0 }, "RetryCount must be greater than zero" },
        { new TestRunnerOptions { RetryDelays = [TimeSpan.FromSeconds(1), TimeSpan.Zero] }, "RetryDelays must be greater than zero" },
        { new TestRunnerOptions { RetryDelays = [TimeSpan.FromSeconds(-1)] }, "RetryDelays must be greater than zero" },
        { new TestRunnerOptions { BackupInterval = TimeSpan.Zero }, "BackupInterval must be greater than zero" },
        { new TestRunnerOptions { ExecutionPolicy = ExecutionPolicy.Sequential, WorkersCount = 2, RetryCount = 1 }, "Sequential execution policy cannot be used with more than one worker" },
        { new TestRunnerOptions { ExecutionPolicy = ExecutionPolicy.Sequential, PerBufferCount = 5, RetryCount = 1 }, "Sequential execution policy requires PerBufferCount to be equal to BufferSize" },
    };

    [Fact]
    public void Validate_ShouldWarn_WhenSequentialWithoutRetryLimits()
    {
        // Arrange
        var options = new TestRunnerOptions { ExecutionPolicy = ExecutionPolicy.Sequential };

        // Act
        var results = Validate(options);

        // Assert
        results.Should().ContainSingle().Which.Should().BeOfType<ValidateResult.Warning>()
            .Which.Message.Should().StartWith("Sequential execution policy without RetryCount and RetryDelays");
    }

    [Fact]
    public void Validate_ShouldNotWarn_WhenSequentialWithRetryCount()
    {
        // Arrange
        var options = new TestRunnerOptions { ExecutionPolicy = ExecutionPolicy.Sequential, RetryCount = 3 };

        // Act & Assert
        Validate(options).Should().BeEmpty();
    }

    [Theory]
    [InlineData(ScopeLifetime.PerGroup, 1)]
    [InlineData(ScopeLifetime.PerSession, 1)]
    [InlineData(ScopeLifetime.PerBatch, 0)]
    public void Validate_ShouldWarn_WhenPartitionKeySharesHandler(ScopeLifetime lifetime, int expected)
    {
        // Arrange
        var options = new TestRunnerOptions { ScopeLifetime = lifetime, PartitionKey = payload => payload };

        // Act
        var warnings = Validate(options).OfType<ValidateResult.Warning>();

        // Assert
        warnings.Should().HaveCount(expected);
    }

    private static ValidateResult[] Validate(TestRunnerOptions options)
        => new OutboxOptionsValidator<object>().Validate(options).ToArray();

    private static string[] Failures(TestRunnerOptions options)
        => Validate(options).OfType<ValidateResult.Failure>().Select(f => f.Error).ToArray();

    public sealed class TestRunnerOptions : IOutboxRunnerOptions<object>
    {
        public string? Identifier => null;
        public TimeSpan PollDelay { get; init; } = TimeSpan.FromSeconds(1);
        public int BufferSize { get; init; } = 10;
        public TimeSpan HandleTimeout { get; init; } = TimeSpan.FromSeconds(1);
        public bool IsEnabled => true;
        public int WorkersCount { get; init; } = 1;
        public int PerBufferCount { get; init; } = 10;
        public DeletePolicy DeletePolicy => DeletePolicy.Safe;
        public TimeSpan FailureDelay { get; init; } = TimeSpan.FromSeconds(1);
        public int? RetryCount { get; init; }
        public TimeSpan[] RetryDelays { get; init; } = [];
        public TimeSpan Visibility { get; init; } = TimeSpan.FromSeconds(1);
        public TimeSpan? BackupInterval { get; init; }
        public TimeSpan CleanupInterval { get; init; } = TimeSpan.FromSeconds(1);
        public ExecutionPolicy ExecutionPolicy { get; init; } = ExecutionPolicy.Parallel;
        public TimeSpan PolicyTimeout { get; init; } = TimeSpan.FromSeconds(1);
        public DeletePolicy CancellationPolicy => DeletePolicy.Safe;
        public ScopeLifetime ScopeLifetime { get; init; } = ScopeLifetime.PerBatch;
        public Func<object, object?>? PartitionKey { get; init; }
    }
}
