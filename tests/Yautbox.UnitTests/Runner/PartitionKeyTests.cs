using System;
using System.Collections.Concurrent;
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
using Yautbox.Runner.Infrastructure;
using Yautbox.Runner.Options;
using Yautbox.UnitTests.Runner.Shared;
using StoredMessage = Yautbox.Entities.OutboxMessage<Yautbox.UnitTests.Runner.PartitionKeyTests.TestMessage>;

namespace Yautbox.UnitTests.Runner;

/// <summary>
/// Covers how the runner splits the messages of one cycle into <c>HandleAsync</c> calls when
/// <see cref="IOutboxRunnerOptions{TPayload}.PartitionKey"/> is set: groups by key first, then chunks each group by
/// <see cref="IOutboxRunnerOptions{TPayload}.PerBufferCount"/>, and handles all chunks concurrently, under
/// <see cref="ExecutionPolicy.Sequential"/> as well.
/// </summary>
public sealed class PartitionKeyTests
{
    [Fact]
    public async Task TryProcessingMessages_WithoutPartitionKey_ChunksByPerBufferCountOnly()
    {
        // Arrange
        var handler = new RecordingHandler();
        var messages = BuildMessages("a", "b", "a", "b", "c");
        var options = new HarnessOptions<TestMessage> { PerBufferCount = 2 };

        // Act
        await ProcessAsync(CreateRunner(handler), options, messages);

        // Assert
        handler.Calls.Select(c => c.Count).Should().BeEquivalentTo([2, 2, 1]);
    }

    [Fact]
    public async Task TryProcessingMessages_WithPartitionKey_PutsOnlyEqualKeysIntoOneCall()
    {
        // Arrange
        var handler = new RecordingHandler();
        var messages = BuildMessages("a", "b", "a", "c", "b", "a");
        var options = new HarnessOptions<TestMessage> { PartitionKey = m => m.Key };

        // Act
        await ProcessAsync(CreateRunner(handler), options, messages);

        // Assert
        handler.Calls
            .Select(c => c.Select(m => m.Key).Distinct().Single())
            .Should().BeEquivalentTo(["a", "b", "c"]);

        handler.Calls
            .SelectMany(c => c.Select(m => m.Id))
            .Should().BeEquivalentTo(messages.Select(m => m.Payload.Id), "every fetched message is handled exactly once");
    }

    [Fact]
    public async Task TryProcessingMessages_WithPartitionKey_SplitsGroupLargerThanPerBufferCount()
    {
        // Arrange
        var handler = new RecordingHandler();
        var messages = BuildMessages("a", "a", "a", "a", "a", "b");
        var options = new HarnessOptions<TestMessage> { PerBufferCount = 2, PartitionKey = m => m.Key };

        // Act
        await ProcessAsync(CreateRunner(handler), options, messages);

        // Assert
        handler.Calls
            .Select(c => (Key: c.Select(m => m.Key).Distinct().Single(), c.Count))
            .Should().BeEquivalentTo([("a", 2), ("a", 2), ("a", 1), ("b", 1)]);
    }

    [Fact]
    public async Task TryProcessingMessages_WithPartitionKey_ComparesKeysByValue()
    {
        // Arrange
        var handler = new RecordingHandler();
        var messages = BuildMessages("a", "b", "a", "b");

        // A fresh boxed int for every message: reference equality would put each message into its own call.
        var options = new HarnessOptions<TestMessage> { PartitionKey = m => m.Id % 2 };

        // Act
        await ProcessAsync(CreateRunner(handler), options, messages);

        // Assert
        handler.Calls.Should().HaveCount(2).And.AllSatisfy(c => c.Should().HaveCount(2));
    }

    [Fact]
    public async Task TryProcessingMessages_WithPartitionKey_GroupsNullKeysSeparatelyFromAnyValue()
    {
        // Arrange
        var handler = new RecordingHandler();
        var messages = BuildMessages(null, "-1", null, "-1");
        var options = new HarnessOptions<TestMessage> { PartitionKey = m => m.Key is null ? null : -1 };

        // Act
        await ProcessAsync(CreateRunner(handler), options, messages);

        // Assert
        handler.Calls
            .Select(c => c.Select(m => m.Key).Distinct().Single())
            .Should().BeEquivalentTo([null, "-1"]);
    }

    [Theory]
    [InlineData(ExecutionPolicy.Parallel)]
    [InlineData(ExecutionPolicy.Sequential)]
    public async Task TryProcessingMessages_WithPartitionKey_HandlesGroupsConcurrently(ExecutionPolicy policy)
    {
        // Arrange
        var handler = new RecordingHandler(expectedConcurrency: 3);
        var messages = BuildMessages("a", "b", "c", "a", "b", "c");
        var options = new HarnessOptions<TestMessage> { ExecutionPolicy = policy, PartitionKey = m => m.Key };

        // Act
        await ProcessAsync(CreateRunner(handler), options, messages);

        // Assert
        handler.Calls.Should().HaveCount(3);
        handler.MaxConcurrency.Should().Be(3, "every group of the cycle is handled at the same time");
    }

    [Fact]
    public async Task TryProcessingMessages_WithPartitionKeyUnderSequential_RequestsTheScopeOncePerCycle()
    {
        // Arrange
        var handler = new RecordingHandler();
        var messages = BuildMessages("a", "b", "c");
        var policyFactory = RunnerHarness.PolicyFactory(RunnerHarness.EmptyScope());
        var options = new HarnessOptions<TestMessage> { ExecutionPolicy = ExecutionPolicy.Sequential, PartitionKey = m => m.Key };

        // Act
        await ProcessAsync(CreateRunner(handler), options, messages, policyFactory);

        // Assert
        await policyFactory.Received(1).TryCreateAsync(
            RunnerHarness.Identifier,
            ExecutionPolicy.Sequential,
            Arg.Any<TimeSpan>(),
            Arg.Any<CancellationToken>());

        handler.Calls.Should().HaveCount(3);
    }

    [Fact]
    public async Task TryProcessingMessages_WhenPartitionKeyThrows_FailsTheCycleWithoutHandlingOrPersisting()
    {
        // Arrange
        var handler = new RecordingHandler();
        var provider = RunnerHarness.Provider<TestMessage>(BuildMessages("a", "b"));
        var options = new HarnessOptions<TestMessage> { PartitionKey = _ => throw new InvalidOperationException("key") };

        // Act
        var act = () => RunnerHarness.TryProcessAsync(CreateRunner(handler), provider, options);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("key");

        handler.Calls.Should().BeEmpty();

        await provider.DidNotReceiveWithAnyArgs().RetryAsync<TestMessage>(default!, default!, default);
        await provider.DidNotReceiveWithAnyArgs().DeleteAsync(default!, default!, default, default);
    }

    [Fact]
    public async Task TryProcessingMessages_WithPartitionKeyAndBatchScope_ResolvesHandlerPerCall()
    {
        // Arrange
        var handlers = new ConcurrentBag<RecordingHandler>();
        var options = new HarnessOptions<TestMessage> { ScopeLifetime = ScopeLifetime.PerBatch, PartitionKey = m => m.Key };

        // Act
        await ProcessAsync(CreateRunner(handlers), options, BuildMessages("a", "b", "c"));

        // Assert
        handlers.Should().HaveCount(3).And.AllSatisfy(h => h.Calls.Should().ContainSingle());
    }

    [Fact]
    public async Task TryProcessingMessages_WithPartitionKeyAndGroupScope_SharesOneHandlerBetweenCalls()
    {
        // Arrange
        var handlers = new ConcurrentBag<RecordingHandler>();
        var options = new HarnessOptions<TestMessage> { ScopeLifetime = ScopeLifetime.PerGroup, PartitionKey = m => m.Key };

        // Act
        await ProcessAsync(CreateRunner(handlers), options, BuildMessages("a", "b", "c"));

        // Assert
        handlers.Should().ContainSingle().Which.Calls.Should().HaveCount(3);
    }

    private static async Task ProcessAsync(
        OutboxHandlerRunner<RecordingHandler, TestMessage> runner,
        HarnessOptions<TestMessage> options,
        StoredMessage[] messages,
        IPolicyFactory? policyFactory = null)
    {
        var processed = await RunnerHarness.TryProcessAsync(runner, RunnerHarness.Provider<TestMessage>(messages), options, policyFactory);

        processed.Should().BeTrue("the handler succeeds, so the cycle must report progress");
    }

    private static OutboxHandlerRunner<RecordingHandler, TestMessage> CreateRunner(RecordingHandler handler)
        => RunnerHarness.CreateRunner<RecordingHandler, TestMessage>(
            new ServiceCollection().AddSingleton(handler).BuildServiceProvider());

    private static OutboxHandlerRunner<RecordingHandler, TestMessage> CreateRunner(ConcurrentBag<RecordingHandler> handlers)
        => RunnerHarness.CreateRunner<RecordingHandler, TestMessage>(
            new ServiceCollection()
                .AddScoped(_ =>
                {
                    var handler = new RecordingHandler();
                    handlers.Add(handler);
                    return handler;
                })
                .BuildServiceProvider());

    private static StoredMessage[] BuildMessages(params string?[] keys)
        => RunnerHarness.Messages(keys.Select((key, i) => new TestMessage(i + 1, key)));

    public sealed record TestMessage(int Id, string? Key);

    /// <summary>
    /// Records the payloads of every call. With <c>expectedConcurrency</c> each call waits until that many calls are
    /// in flight, so a runner handling the chunks one by one shows up as a lower <see cref="MaxConcurrency"/> after
    /// the wait times out rather than as a hang.
    /// </summary>
    public sealed class RecordingHandler(int expectedConcurrency = 1) : IOutboxHandler<TestMessage>
    {
        private readonly TaskCompletionSource _allEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _current;
        private int _max;

        public ConcurrentBag<IReadOnlyCollection<TestMessage>> Calls { get; } = [];

        public int MaxConcurrency => Volatile.Read(ref _max);

        public RecordingHandler() : this(1)
        {
        }

        public async Task HandleAsync(IEnumerable<OutboxMessage<TestMessage>> messages, CancellationToken cancellationToken)
        {
            Calls.Add([.. messages.Select(m => m.Payload)]);

            var current = Interlocked.Increment(ref _current);

            int max;
            while (current > (max = Volatile.Read(ref _max)))
                Interlocked.CompareExchange(ref _max, current, max);

            if (current >= expectedConcurrency)
                _allEntered.TrySetResult();

            try
            {
                await _allEntered.Task.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);
            }
            catch (TimeoutException)
            {
                // Leave the assertion to MaxConcurrency, which names the actual number of concurrent calls.
            }
            finally
            {
                Interlocked.Decrement(ref _current);
            }
        }
    }
}
