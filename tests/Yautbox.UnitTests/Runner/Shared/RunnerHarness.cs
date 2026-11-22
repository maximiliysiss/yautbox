using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Yautbox.Entities;
using Yautbox.Infrastructure.DateTime;
using Yautbox.Infrastructure.Waiter;
using Yautbox.Metrics;
using Yautbox.Provider;
using Yautbox.Registy;
using Yautbox.Runner;
using Yautbox.Runner.Infrastructure;
using Yautbox.Runner.Options;
using Yautbox.Runner.Validation;
using Yautbox.Tracing;

namespace Yautbox.UnitTests.Runner.Shared;

/// <summary>
/// Builds <see cref="OutboxHandlerRunner{THandler,TPayload}"/> and <see cref="OutboxCleanerRunner{THandler,TPayload}"/>
/// instances with substituted infrastructure, and invokes the private processing seam.
/// </summary>
internal static class RunnerHarness
{
    public const string Identifier = "test-message";

    public static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public static OutboxHandlerRunner<THandler, TPayload> CreateRunner<THandler, TPayload>(
        IServiceProvider serviceProvider,
        IOutboxRunnerOptions<TPayload>? options = null,
        IEnumerable<IOutboxOptionsValidator<TPayload>>? validators = null,
        ILogger<OutboxHandlerRunner<THandler, TPayload>>? logger = null,
        IMetricsHandler? metrics = null)
        where THandler : class, Handlers.IOutboxHandler<TPayload>
        => new(
            serviceProvider: serviceProvider,
            options: Monitor(options ?? new HarnessOptions<TPayload>()),
            readinessWaiter: Substitute.For<IInfrastructureReadinessWaiter>(),
            logger: logger ?? NullLogger<OutboxHandlerRunner<THandler, TPayload>>.Instance,
            dateTimeProvider: DateTimeProvider(),
            metricsHandler: metrics ?? Substitute.For<IMetricsHandler>(),
            tracer: Tracer(),
            validators: validators ?? []);

    public static OutboxCleanerRunner<THandler, TPayload> CreateCleaner<THandler, TPayload>(
        IServiceProvider serviceProvider,
        IOutboxRunnerOptions<TPayload> options,
        IEnumerable<IOutboxOptionsValidator<TPayload>> validators,
        ILogger<OutboxCleanerRunner<THandler, TPayload>> logger)
        where THandler : class, Handlers.IOutboxHandler<TPayload>
        => new(
            logger: logger,
            options: Monitor(options),
            serviceProvider: serviceProvider,
            readinessWaiter: Substitute.For<IInfrastructureReadinessWaiter>(),
            dateTimeProvider: DateTimeProvider(),
            metricsHandler: Substitute.For<IMetricsHandler>(),
            tracer: Tracer(),
            validators: validators);

    public static Task<bool> TryProcessAsync<THandler, TPayload>(
        OutboxHandlerRunner<THandler, TPayload> runner,
        IOutboxProvider provider,
        IOutboxRunnerOptions<TPayload> options,
        IPolicyFactory? policyFactory = null,
        CancellationToken cancellationToken = default)
        where THandler : class, Handlers.IOutboxHandler<TPayload>
    {
        var method = typeof(OutboxHandlerRunner<THandler, TPayload>)
            .GetMethod("TryProcessingMessagesAsync", BindingFlags.Instance | BindingFlags.NonPublic);

        method.Should().NotBeNull("the tested seam must exist; rename the test if the method is renamed");

        return (Task<bool>)method!.Invoke(
            runner,
            [Identifier, provider, null, options, policyFactory ?? PolicyFactory(EmptyScope()), cancellationToken])!;
    }

    public static IPolicyFactory PolicyFactory(IOutboxPolicyScope? scope)
    {
        var policyFactory = Substitute.For<IPolicyFactory>();
        policyFactory
            .TryCreateAsync(Arg.Any<string>(), Arg.Any<ExecutionPolicy>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(scope));

        return policyFactory;
    }

    public static IOutboxPolicyScope EmptyScope()
    {
        var scope = Substitute.For<IOutboxPolicyScope>();
        scope.HandleLostToken.Returns(CancellationToken.None);
        return scope;
    }

    /// <summary>
    /// A provider whose writes behave like a real storage: they refuse to run on a cancelled token.
    /// </summary>
    public static IOutboxProvider Provider<TPayload>(IReadOnlyCollection<OutboxMessage<TPayload>> fetched)
    {
        var provider = Substitute.For<IOutboxProvider>();
        provider
            .GetAsync<TPayload>(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(fetched));
        provider
            .RetryAsync(Arg.Any<string>(), Arg.Any<IReadOnlyCollection<OutboxMessage<TPayload>>>(), Arg.Any<CancellationToken>())
            .Returns(ci => CompleteUnlessCancelled(ci.Arg<CancellationToken>()));
        provider
            .DeleteAsync(Arg.Any<string>(), Arg.Any<IReadOnlyCollection<OutboxMessageId>>(), Arg.Any<DeletePolicy>(), Arg.Any<CancellationToken>())
            .Returns(ci => CompleteUnlessCancelled(ci.Arg<CancellationToken>()));

        return provider;
    }

    public static OutboxMessage<TPayload>[] Messages<TPayload>(IEnumerable<TPayload> payloads, int attempt = 0)
        => payloads
            .Select((payload, i) => new OutboxMessage<TPayload>(
                Id: new OutboxMessageId(i + 1),
                Payload: payload,
                Attempt: attempt,
                ScheduledAt: null,
                CreatedAt: Now))
            .ToArray();

    public static async Task WaitUntilAsync(Func<bool> condition, TimeSpan? limit = null)
    {
        using var timeout = new CancellationTokenSource(limit ?? TimeSpan.FromSeconds(10));

        while (!condition())
            await Task.Delay(TimeSpan.FromMilliseconds(20), timeout.Token);
    }

    private static Task CompleteUnlessCancelled(CancellationToken cancellationToken)
        => cancellationToken.IsCancellationRequested ? Task.FromCanceled(cancellationToken) : Task.CompletedTask;

    private static IOptionsMonitor<IOutboxRunnerOptions<TPayload>> Monitor<TPayload>(IOutboxRunnerOptions<TPayload> options)
    {
        var monitor = Substitute.For<IOptionsMonitor<IOutboxRunnerOptions<TPayload>>>();
        monitor.CurrentValue.Returns(options);
        return monitor;
    }

    private static IDateTimeProvider DateTimeProvider()
    {
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.GetNow().Returns(Now);
        return dateTimeProvider;
    }

    private static IOutboxTracer Tracer()
    {
        var tracer = Substitute.For<IOutboxTracer>();
        tracer.StartHandle(Arg.Any<string>(), Arg.Any<int>()).Returns(Substitute.For<IOutboxTraceScope>());
        tracer.StartPersist(Arg.Any<string>()).Returns(Substitute.For<IOutboxTraceScope>());
        tracer.StartCleanup(Arg.Any<string>()).Returns(Substitute.For<IOutboxTraceScope>());
        return tracer;
    }
}

/// <summary>
/// A fixed registry: every payload type maps to <see cref="RunnerHarness.Identifier"/>.
/// </summary>
internal sealed class HarnessRegistry : IOutboxRegistry
{
    public string GetIdentifier(Type type) => RunnerHarness.Identifier;

    public DeletePolicy GetCancellationPolicy<T>() => DeletePolicy.Safe;
}

public sealed class HarnessOptions<TPayload> : IOutboxRunnerOptions<TPayload>
{
    public string? Identifier => null;
    public TimeSpan PollDelay { get; init; } = TimeSpan.FromMilliseconds(50);
    public int BufferSize { get; init; } = 100;
    public TimeSpan HandleTimeout { get; init; } = TimeSpan.FromMinutes(1);
    public bool IsEnabled { get; init; } = true;
    public int WorkersCount { get; init; } = 1;
    public int PerBufferCount { get; init; } = 100;
    public DeletePolicy DeletePolicy => DeletePolicy.Safe;
    public TimeSpan FailureDelay { get; init; } = TimeSpan.FromSeconds(2);
    public int? RetryCount { get; init; }
    public TimeSpan[] RetryDelays { get; init; } = [];
    public TimeSpan Visibility => TimeSpan.FromMinutes(1);
    public TimeSpan? BackupInterval { get; init; }
    public TimeSpan CleanupInterval => TimeSpan.FromDays(1);
    public ExecutionPolicy ExecutionPolicy { get; init; } = ExecutionPolicy.Parallel;
    public TimeSpan PolicyTimeout => TimeSpan.FromSeconds(1);
    public DeletePolicy CancellationPolicy => DeletePolicy.Safe;
    public ScopeLifetime ScopeLifetime { get; init; } = ScopeLifetime.PerBatch;
    public Func<TPayload, object?>? PartitionKey { get; init; }
}

/// <summary>
/// Records every log entry with its level and formatted message.
/// </summary>
internal sealed class RecordingLogger<T> : ILogger<T>
{
    private readonly List<Entry> _entries = [];

    public IReadOnlyList<Entry> Entries
    {
        get
        {
            lock (_entries)
                return _entries.ToArray();
        }
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        lock (_entries)
            _entries.Add(new Entry(logLevel, formatter(state, exception)));
    }

    public sealed record Entry(LogLevel Level, string Message);
}
