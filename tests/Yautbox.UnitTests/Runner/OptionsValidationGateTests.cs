using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;
using Yautbox.Handlers;
using Yautbox.Infrastructure.Hosted;
using Yautbox.Metrics;
using Yautbox.Provider;
using Yautbox.Registy;
using Yautbox.Runner;
using Yautbox.Runner.Infrastructure;
using Yautbox.Runner.Options;
using Yautbox.Runner.Validation;
using Yautbox.UnitTests.Runner.Shared;

namespace Yautbox.UnitTests.Runner;

/// <summary>
/// Covers the gate both runners pass before processing: the results of every registered
/// <see cref="IOutboxOptionsValidator{TPayload}"/> are combined, any failure abandons the loop with
/// <see cref="InterruptLoopException"/> carrying all of them, warnings are logged without stopping anything, and
/// a runner that does not process (disabled, or cleanup off) does not validate at all.
/// Also covers the cycle metrics reported by the worker loop.
/// </summary>
public sealed class OptionsValidationGateTests
{
    private static readonly TimeSpan WaitLimit = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task HandlerRunner_ShouldFaultWithEveryFailure_WhenValidatorsReportFailures()
    {
        // Arrange
        var policyFactory = Substitute.For<IPolicyFactory>();

        var runner = RunnerHarness.CreateRunner<Handler, TestMessage>(
            ServiceProvider(policyFactory),
            validators:
            [
                new StubValidator(new ValidateResult.Failure("first")),
                new StubValidator(new ValidateResult.Warning("note"), new ValidateResult.Failure("second")),
            ]);

        // Act
        await runner.StartAsync(CancellationToken.None);

        var execution = () => runner.ExecuteTask!.WaitAsync(WaitLimit);

        // Assert
        var exception = await execution.Should().ThrowExactlyAsync<InterruptLoopException>();
        exception.Which.Message.Should().Be($"first{Environment.NewLine}second");

        await policyFactory.DidNotReceiveWithAnyArgs().TryCreateAsync(default!, default, default, default);
    }

    [Fact]
    public async Task HandlerRunner_ShouldLogWarningsAndProcess_WhenValidatorsReportOnlyWarnings()
    {
        // Arrange
        var policyFactory = RunnerHarness.PolicyFactory(scope: null);
        var logger = new RecordingLogger<OutboxHandlerRunner<Handler, TestMessage>>();

        var runner = RunnerHarness.CreateRunner<Handler, TestMessage>(
            ServiceProvider(policyFactory),
            logger: logger,
            validators: [new StubValidator(new ValidateResult.Warning("first")), new StubValidator(new ValidateResult.Warning("second"))]);

        // Act
        await runner.StartAsync(CancellationToken.None);

        try
        {
            // Assert: reaching the execution scope means the worker went on to process.
            await RunnerHarness.WaitUntilAsync(() => policyFactory.ReceivedCalls().Any(), WaitLimit);

            runner.ExecuteTask!.IsCompleted.Should().BeFalse();

            logger.Entries.Should().ContainSingle(e => e.Level == LogLevel.Warning)
                .Which.Message.Should().Contain($"first{Environment.NewLine}second");
        }
        finally
        {
            await runner.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task HandlerRunner_ShouldNotValidate_WhenDisabled()
    {
        // Arrange
        var validator = new StubValidator(new ValidateResult.Failure("never reported"));
        var logger = new RecordingLogger<OutboxHandlerRunner<Handler, TestMessage>>();

        var runner = RunnerHarness.CreateRunner<Handler, TestMessage>(
            ServiceProvider(Substitute.For<IPolicyFactory>()),
            options: new HarnessOptions<TestMessage> { IsEnabled = false },
            logger: logger,
            validators: [validator]);

        // Act
        await runner.StartAsync(CancellationToken.None);

        try
        {
            // Assert
            await RunnerHarness.WaitUntilAsync(() => logger.Entries.Any(e => e.Message.Contains("is disabled")), WaitLimit);

            validator.Calls.Should().Be(0);
            runner.ExecuteTask!.IsCompleted.Should().BeFalse();
        }
        finally
        {
            await runner.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task CleanerRunner_ShouldFaultWithEveryFailure_WhenCleanupIsEnabledAndValidatorsReportFailures()
    {
        // Arrange
        var cleaner = RunnerHarness.CreateCleaner<Handler, TestMessage>(
            ServiceProvider(Substitute.For<IPolicyFactory>()),
            new HarnessOptions<TestMessage> { BackupInterval = TimeSpan.FromDays(1) },
            [new StubValidator(new ValidateResult.Failure("first"), new ValidateResult.Failure("second"))],
            new RecordingLogger<OutboxCleanerRunner<Handler, TestMessage>>());

        // Act
        await cleaner.StartAsync(CancellationToken.None);

        var execution = () => cleaner.ExecuteTask!.WaitAsync(WaitLimit);

        // Assert
        var exception = await execution.Should().ThrowExactlyAsync<InterruptLoopException>();
        exception.Which.Message.Should().Be($"first{Environment.NewLine}second");
    }

    [Fact]
    public async Task CleanerRunner_ShouldNotValidate_WhenCleanupIsDisabled()
    {
        // Arrange
        var validator = new StubValidator(new ValidateResult.Failure("never reported"));
        var logger = new RecordingLogger<OutboxCleanerRunner<Handler, TestMessage>>();

        var cleaner = RunnerHarness.CreateCleaner<Handler, TestMessage>(
            ServiceProvider(Substitute.For<IPolicyFactory>()),
            new HarnessOptions<TestMessage> { BackupInterval = null },
            [validator],
            logger);

        // Act
        await cleaner.StartAsync(CancellationToken.None);

        try
        {
            // Assert
            await RunnerHarness.WaitUntilAsync(() => logger.Entries.Any(e => e.Message.Contains("cleanup is disabled")), WaitLimit);

            validator.Calls.Should().Be(0);
            cleaner.ExecuteTask!.IsCompleted.Should().BeFalse();
        }
        finally
        {
            await cleaner.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task HandlerRunner_ShouldReportCycled_WhenLoopCompletes()
    {
        // Arrange
        var metrics = Substitute.For<IMetricsHandler>();
        var runner = RunnerHarness.CreateRunner<Handler, TestMessage>(
            ServiceProvider(RunnerHarness.PolicyFactory(scope: null)),
            metrics: metrics);

        // Act
        await runner.StartAsync(CancellationToken.None);

        try
        {
            // Assert
            await RunnerHarness.WaitUntilAsync(() => metrics.ReceivedCalls().Any(c => c.GetMethodInfo().Name == nameof(IMetricsHandler.CycledAsync)), WaitLimit);

            await metrics.DidNotReceiveWithAnyArgs().CycledFailedAsync(default!, default);
        }
        finally
        {
            await runner.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task HandlerRunner_ShouldReportCycledFailed_WhenLoopThrows()
    {
        // Arrange
        var metrics = Substitute.For<IMetricsHandler>();
        var policyFactory = Substitute.For<IPolicyFactory>();
        policyFactory
            .TryCreateAsync(Arg.Any<string>(), Arg.Any<ExecutionPolicy>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<IOutboxPolicyScope?>(new InvalidOperationException("storage is down")));

        var runner = RunnerHarness.CreateRunner<Handler, TestMessage>(
            ServiceProvider(policyFactory),
            options: new HarnessOptions<TestMessage> { FailureDelay = TimeSpan.FromMilliseconds(10) },
            metrics: metrics);

        // Act
        await runner.StartAsync(CancellationToken.None);

        try
        {
            // Assert
            await RunnerHarness.WaitUntilAsync(() => metrics.ReceivedCalls().Any(c => c.GetMethodInfo().Name == nameof(IMetricsHandler.CycledFailedAsync)), WaitLimit);

            await metrics.Received().CycledFailedAsync(RunnerHarness.Identifier, Arg.Any<CancellationToken>());
            await metrics.DidNotReceiveWithAnyArgs().CycledAsync(default!, default);
        }
        finally
        {
            await runner.StopAsync(CancellationToken.None);
        }
    }

    private static IServiceProvider ServiceProvider(IPolicyFactory policyFactory)
        => new ServiceCollection()
            .AddSingleton<IOutboxRegistry, HarnessRegistry>()
            .AddSingleton(Substitute.For<IOutboxProvider>())
            .AddSingleton(policyFactory)
            .AddScoped<Handler>()
            .BuildServiceProvider();

    public sealed record TestMessage(int Id);

    public sealed class Handler : IOutboxHandler<TestMessage>
    {
        public Task HandleAsync(IEnumerable<OutboxMessage<TestMessage>> messages, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }

    private sealed class StubValidator(params ValidateResult[] results) : IOutboxOptionsValidator<TestMessage>
    {
        private int _calls;

        public int Calls => Volatile.Read(ref _calls);

        public IEnumerable<ValidateResult> Validate(IOutboxRunnerOptions<TestMessage> options)
        {
            Interlocked.Increment(ref _calls);
            return results;
        }
    }
}
