using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;
using Yautbox.Handlers;
using Yautbox.Provider;
using Yautbox.Runner;
using Yautbox.Runner.Options;
using Yautbox.UnitTests.Runner.Shared;
using StoredMessage = Yautbox.Entities.OutboxMessage<Yautbox.UnitTests.Runner.CycleTimeoutTests.TestMessage>;

namespace Yautbox.UnitTests.Runner;

/// <summary>
/// Covers what the runner persists when a cycle runs past <see cref="IOutboxRunnerOptions{TPayload}.HandleTimeout"/>:
/// the handler is cancelled and fails its messages, and that failure has to reach the storage as a scheduled retry.
/// The timeout bounds fetching and handling, not the persist that records the outcome.
/// </summary>
public sealed class CycleTimeoutTests
{
    [Fact]
    public async Task TryProcessingMessages_WhenTimeoutCancelsHandler_PersistsRetryForAllFetchedMessages()
    {
        // Arrange
        var fetched = RunnerHarness.Messages(Enumerable.Range(1, 3).Select(i => new TestMessage(i)), attempt: 1);
        var provider = RunnerHarness.Provider<TestMessage>(fetched);
        var options = new HarnessOptions<TestMessage> { HandleTimeout = TimeSpan.FromMilliseconds(100) };

        // Act
        var processed = await RunnerHarness.TryProcessAsync(CreateRunner(), provider, options);

        // Assert
        processed.Should().BeFalse("every message of the cycle failed");

        await provider.Received(1).RetryAsync(
            RunnerHarness.Identifier,
            Arg.Is<IReadOnlyCollection<StoredMessage>>(m =>
                m.Select(x => x.Id).OrderBy(id => id.Value).SequenceEqual(fetched.Select(x => x.Id)) &&
                m.All(x => x.Attempt == 2 && x.ScheduledAt == RunnerHarness.Now.Add(options.FailureDelay))),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TryProcessingMessages_WhenTimeoutCancelsHandlerUnderSequential_PersistsImmediateRetry()
    {
        // Arrange
        var fetched = RunnerHarness.Messages(Enumerable.Range(1, 2).Select(i => new TestMessage(i)));
        var provider = RunnerHarness.Provider<TestMessage>(fetched);
        var options = new HarnessOptions<TestMessage>
        {
            HandleTimeout = TimeSpan.FromMilliseconds(100),
            ExecutionPolicy = ExecutionPolicy.Sequential
        };

        // Act
        await RunnerHarness.TryProcessAsync(CreateRunner(), provider, options);

        // Assert
        await provider.Received(1).RetryAsync(
            RunnerHarness.Identifier,
            Arg.Is<IReadOnlyCollection<StoredMessage>>(m =>
                m.Count == fetched.Length && m.All(x => x.Attempt == 1 && x.ScheduledAt == null)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TryProcessingMessages_WhenTimeoutCancelsHandler_DoesNotFailTheCycle()
    {
        // Arrange
        var provider = RunnerHarness.Provider<TestMessage>(RunnerHarness.Messages([new TestMessage(1)]));
        var options = new HarnessOptions<TestMessage> { HandleTimeout = TimeSpan.FromMilliseconds(100) };

        // Act
        var act = () => RunnerHarness.TryProcessAsync(CreateRunner(), provider, options);

        // Assert
        await act.Should().NotThrowAsync("an expired timeout is a handler failure, not a failure of the loop");
    }

    private static OutboxHandlerRunner<WaitForCancellationHandler, TestMessage> CreateRunner()
        => RunnerHarness.CreateRunner<WaitForCancellationHandler, TestMessage>(
            new ServiceCollection().AddScoped<WaitForCancellationHandler>().BuildServiceProvider());

    public sealed record TestMessage(int Id);

    public sealed class WaitForCancellationHandler : IOutboxHandler<TestMessage>
    {
        public Task HandleAsync(IEnumerable<OutboxMessage<TestMessage>> messages, CancellationToken cancellationToken)
            => Task.Delay(Timeout.Infinite, cancellationToken);
    }
}
