using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;
using Yautbox.Entities;
using Yautbox.Handlers;
using Yautbox.Infrastructure.DateTime;
using Yautbox.Infrastructure.Waiter;
using Yautbox.Metrics;
using Yautbox.Provider;
using Yautbox.Runner;
using Yautbox.Runner.Infrastructure;
using Yautbox.Runner.Options;
using Yautbox.Tracing;
using StoredMessage = Yautbox.Entities.OutboxMessage<Yautbox.UnitTests.Runner.OutboxHandlerRunnerTests.TestMessage>;

namespace Yautbox.UnitTests.Runner;

public sealed class OutboxHandlerRunnerTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task TryProcessingMessages_WhenCancelledDuringPersist_RetriesAllFetchedMessages()
    {
        // Arrange
        var fetched = BuildMessages(count: 5);
        var provider = CreateProvider(fetched);
        var runner = CreateRunner();

        using var shutdown = new CancellationTokenSource();
        await shutdown.CancelAsync();

        // Act
        var processed = await InvokeTryProcessingMessagesAsync(runner, provider, shutdown.Token);

        // Assert
        processed.Should().BeFalse("a cancelled persist must not report progress");

        await provider.Received(1).RetryAsync(
            "test-message",
            Arg.Is<IReadOnlyCollection<StoredMessage>>(messages =>
                messages.Select(message => message.Id).OrderBy(id => id.Value).SequenceEqual(
                    fetched.Select(message => message.Id).OrderBy(id => id.Value))),
            Arg.Is<CancellationToken>(token => !token.CanBeCanceled));
    }

    [Fact]
    public async Task TryProcessingMessages_WhenCancelledDuringPersist_DoesNotIncrementAttempt()
    {
        // Arrange
        var fetched = BuildMessages(count: 3, attempt: 2);
        var provider = CreateProvider(fetched);
        var runner = CreateRunner();

        using var shutdown = new CancellationTokenSource();
        await shutdown.CancelAsync();

        // Act
        await InvokeTryProcessingMessagesAsync(runner, provider, shutdown.Token);

        // Assert
        await provider.Received(1).RetryAsync(
            "test-message",
            Arg.Is<IReadOnlyCollection<StoredMessage>>(messages =>
                messages.All(message => message.Attempt == 2)),
            Arg.Is<CancellationToken>(token => !token.CanBeCanceled));
    }

    private static IOutboxProvider CreateProvider(IReadOnlyCollection<StoredMessage> fetched)
    {
        var provider = Substitute.For<IOutboxProvider>();
        provider
            .GetAsync<TestMessage>(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(fetched));
        provider
            .DeleteAsync(
                Arg.Any<string>(),
                Arg.Any<IReadOnlyCollection<OutboxMessageId>>(),
                Arg.Any<DeletePolicy>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new OperationCanceledException()));

        return provider;
    }

    private static OutboxHandlerRunner<SucceedingHandler, TestMessage> CreateRunner()
    {
        var services = new ServiceCollection()
            .AddScoped<SucceedingHandler>()
            .BuildServiceProvider();

        var tracer = Substitute.For<IOutboxTracer>();
        tracer.StartHandle(Arg.Any<string>(), Arg.Any<int>()).Returns(Substitute.For<IOutboxTraceScope>());
        tracer.StartPersist(Arg.Any<string>()).Returns(Substitute.For<IOutboxTraceScope>());

        return new OutboxHandlerRunner<SucceedingHandler, TestMessage>(
            serviceProvider: services,
            options: Substitute.For<IOptionsMonitor<IOutboxRunnerOptions>>(),
            readinessWaiter: Substitute.For<IInfrastructureReadinessWaiter>(),
            logger: NullLogger<OutboxHandlerRunner<SucceedingHandler, TestMessage>>.Instance,
            dateTimeProvider: Substitute.For<IDateTimeProvider>(),
            metricsHandler: Substitute.For<IMetricsHandler>(),
            tracer: tracer);
    }

    private static async Task<bool> InvokeTryProcessingMessagesAsync(
        OutboxHandlerRunner<SucceedingHandler, TestMessage> runner,
        IOutboxProvider provider,
        CancellationToken cancellationToken)
    {
        var method = typeof(OutboxHandlerRunner<SucceedingHandler, TestMessage>)
            .GetMethod("TryProcessingMessagesAsync", BindingFlags.Instance | BindingFlags.NonPublic);

        method.Should().NotBeNull("the graceful-shutdown processing seam must remain available");

        var policyScope = Substitute.For<IAsyncDisposable>();
        var policyFactory = Substitute.For<IPolicyFactory>();
        policyFactory
            .CreateAsync(Arg.Any<string>(), Arg.Any<ExecutionPolicy>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(policyScope));

        var task = (Task<bool>)method!.Invoke(
            runner,
            ["test-message", provider, null, new TestOptions(), policyFactory, cancellationToken])!;

        return await task;
    }

    private static StoredMessage[] BuildMessages(int count, int attempt = 0)
        => Enumerable
            .Range(1, count)
            .Select(id => new StoredMessage(
                Id: new OutboxMessageId(id),
                Payload: new TestMessage(id),
                CreatedAt: Now,
                Attempt: attempt,
                ScheduledAt: null))
            .ToArray();

    public sealed record TestMessage(int Id);

    public sealed class SucceedingHandler : IOutboxHandler<TestMessage>
    {
        public Task HandleAsync(IEnumerable<Handlers.OutboxMessage<TestMessage>> messages, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }

    private sealed class TestOptions : IOutboxRunnerOptions
    {
        public string? Identifier => null;
        public TimeSpan PollDelay => TimeSpan.FromSeconds(1);
        public int BufferSize => 100;
        public TimeSpan HandleTimeout => TimeSpan.FromMinutes(1);
        public bool IsEnabled => true;
        public int WorkersCount => 1;
        public int PerBufferCount => 100;
        public DeletePolicy DeletePolicy => DeletePolicy.Safe;
        public TimeSpan FailureDelay => TimeSpan.FromSeconds(1);
        public TimeSpan Visibility => TimeSpan.FromMinutes(1);
        public TimeSpan? BackupInterval => null;
        public TimeSpan CleanupInterval => TimeSpan.FromDays(1);
        public ExecutionPolicy ExecutionPolicy => ExecutionPolicy.Parallel;
        public TimeSpan PolicyTimeout => TimeSpan.FromMinutes(1);
        public DeletePolicy CancellationPolicy => DeletePolicy.Safe;
        public ScopeLifetime ScopeLifetime => ScopeLifetime.PerBatch;
    }
}
