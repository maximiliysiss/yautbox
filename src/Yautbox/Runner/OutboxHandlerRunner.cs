using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Yautbox.Extensions.DateTime;
using Yautbox.Extensions.Logger;
using Yautbox.Extensions.Options;
using Yautbox.Handlers;
using Yautbox.Infrastructure.DateTime;
using Yautbox.Infrastructure.Hosted;
using Yautbox.Infrastructure.Waiter;
using Yautbox.Metrics;
using Yautbox.Provider;
using Yautbox.Registy;
using Yautbox.Runner.Extensions;
using Yautbox.Runner.Infrastructure;
using Yautbox.Runner.Options;

namespace Yautbox.Runner;

internal class OutboxHandlerRunner<THandler, TPayload> : RestartableService where THandler : class, IOutboxHandler<TPayload>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IInfrastructureReadinessWaiter _readinessWaiter;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IMetricsHandler _metricsHandler;

    private readonly IOptionsMonitor<IOutboxRunnerOptions> _options;

    private readonly ILogger<OutboxHandlerRunner<THandler, TPayload>> _logger;

    public OutboxHandlerRunner(
        IServiceProvider serviceProvider,
        IOptionsMonitor<IOutboxRunnerOptions> options,
        IInfrastructureReadinessWaiter readinessWaiter,
        ILogger<OutboxHandlerRunner<THandler, TPayload>> logger,
        IDateTimeProvider dateTimeProvider,
        IMetricsHandler metricsHandler)
        : base(logger)
    {
        _serviceProvider = serviceProvider;
        _readinessWaiter = readinessWaiter;
        _logger = logger;
        _dateTimeProvider = dateTimeProvider;
        _metricsHandler = metricsHandler;
        _options = options;

        ServiceName = $"{base.ServiceName}[{typeof(TPayload).FullName},{typeof(THandler).FullName}]";
    }

    protected override string ServiceName { get; }

    protected override async Task ExecuteAsync(CancellationTokenSource reloadTokenSource)
    {
        using var optionsRegistration = _options.OnChange(_ => reloadTokenSource.Cancel());
        var cancellationToken = reloadTokenSource.Token;

        await _readinessWaiter.WaitAsync(cancellationToken);

        var options = _options.CurrentValue;

        Action? invalidAction = options switch
        {
            { IsEnabled: false } => () => _logger.DisableOutbox(ServiceName),
            _ when options.Validate() is ValidationResult.FailureValidationResult r
                => () => _logger.InvalidOutboxOptions(ServiceName, r.ErrorMessage),
            _ => null,
        };

        if (invalidAction is not null)
        {
            invalidAction();
            await Task.Delay(Timeout.Infinite, cancellationToken);
            return;
        }

        // Initial jitter to spread a load
        await Task.Delay(TimeSpan.Zero.Jitter(), cancellationToken);

        using var serviceScope = _serviceProvider.CreateScope();

        var registry = serviceScope.ServiceProvider.GetRequiredService<IOutboxRegistry>();

        var identifier = registry.GetIdentifier<TPayload>();

        var workers = Enumerable
            .Range(1, Math.Max(1, options.WorkersCount))
            .Select(_ => WorkerAsync(cancellationToken));

        await Task.WhenAll(workers);

        return;

        async Task WorkerAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var sessionScope = _serviceProvider.CreateScope();

                    var provider = sessionScope.ServiceProvider.GetRequiredService<IOutboxProvider>();
                    var policyFactory = sessionScope.ServiceProvider.GetRequiredService<IPolicyFactory>();

                    var handler = options.ScopeLifetime is ScopeLifetime.PerSession
                        ? sessionScope.ServiceProvider.GetRequiredService<THandler>()
                        : null;

                    await HandleLoopAsync(identifier, provider, handler, options, policyFactory, stoppingToken);

                    await Task.Delay(options.PollDelay.Jitter(), stoppingToken);
                }
                catch (Exception ex) when (ex.IsCancel() && stoppingToken.IsCancellationRequested)
                {
                    // Graceful shutdown
                    return;
                }
                catch (Exception ex)
                {
                    var type = typeof(TPayload);

                    _logger.ErrorOutboxBackgroundService(type.FullName ?? type.Name, ex);

                    // Prevent tight loop on errors
                    await Task.Delay(options.FailureDelay.Jitter(), stoppingToken);
                }
            }
        }
    }

    private async Task HandleLoopAsync(
        string identifier,
        IOutboxProvider provider,
        THandler? handler,
        IOutboxRunnerOptions options,
        IPolicyFactory policyFactory,
        CancellationToken cancellationToken)
    {
        bool succeededProcessing;
        do succeededProcessing = await TryProcessingMessagesAsync(identifier, provider, handler, options, policyFactory, cancellationToken);
        while (succeededProcessing && !cancellationToken.IsCancellationRequested);
    }

    private async Task<bool> TryProcessingMessagesAsync(
        string identifier,
        IOutboxProvider provider,
        THandler? handler,
        IOutboxRunnerOptions options,
        IPolicyFactory policyFactory,
        CancellationToken cancellationToken)
    {
        await using var _ = await policyFactory.CreateAsync(
            identifier: identifier,
            policy: options.ExecutionPolicy,
            timeout: options.PolicyTimeout,
            cancellationToken: cancellationToken);

        var startTimestamp = Stopwatch.GetTimestamp();

        using var groupScope = _serviceProvider.CreateScope();

        handler = options.ScopeLifetime is ScopeLifetime.PerGroup
            ? groupScope.ServiceProvider.GetRequiredService<THandler>()
            : handler;

        var outboxMessages = await provider.GetAsync<TPayload>(
            identifier: identifier,
            count: options.BufferSize,
            visibility: options.Visibility,
            cancellationToken: CancellationToken.None);

        var elapsed = Stopwatch.GetElapsedTime(startTimestamp);

        try
        {
            await _metricsHandler.ReadInAsync(identifier, elapsed, cancellationToken);

            if (outboxMessages.Count is 0)
                return false;

            _logger.OutboxMessagesFetched(identifier, outboxMessages.Count, elapsed);

            using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cancellationTokenSource.CancelAfter(options.HandleTimeout);

            var cycleCancellationToken = cancellationTokenSource.Token;

            var loopTasks = outboxMessages
                .Chunk(options.PerBufferCount)
                .Select(g => LoopAsync(g, cycleCancellationToken))
                .ToArray();

            if (loopTasks is [])
                return false;

            var contexts = await Task.WhenAll(loopTasks);

            var failedCount = contexts.Count(c => c.IsFailed);
            if (failedCount is not 0)
                await _metricsHandler.ErrorsAsync(identifier, failedCount, cancellationToken);

            var toRetryMessages = contexts
                .SelectMany(c => c.Retries.Select(MapRetry))
                .ToArray();

            var toDeleteMessages = contexts
                .SelectMany(c => c.Success)
                .ToArray();

            var toExceededMessages = contexts
                .SelectMany(c => c.RetryExceeded)
                .ToArray();

            await provider.RetryAsync(
                identifier: identifier,
                messages: toRetryMessages,
                cancellationToken: cancellationToken);

            await provider.DeleteAsync(
                identifier: identifier,
                ids: [.. toDeleteMessages, .. toExceededMessages],
                policy: options.DeletePolicy,
                cancellationToken: cancellationToken);

            await _metricsHandler.RetriedAsync(identifier, toRetryMessages.Length, cancellationToken);
            await _metricsHandler.DeletedAsync(identifier, toDeleteMessages.Length + toExceededMessages.Length, cancellationToken);

            _logger.OutboxMessagesProcessed(
                identifier: identifier,
                fetchedCount: outboxMessages.Count,
                retryCount: toRetryMessages.Length,
                deleteCount: toDeleteMessages.Length + toExceededMessages.Length);

            return contexts.Any(c => c.IsSuccess);
        }
        catch (Exception ex) when (ex.IsCancel() && cancellationToken.IsCancellationRequested)
        {
            if (outboxMessages.Count is not 0)
            {
                _logger.OutboxRetrying(identifier, outboxMessages.Count);

                await provider.RetryAsync(
                    identifier: identifier,
                    messages: outboxMessages,
                    cancellationToken: CancellationToken.None);
            }

            return false;
        }

        async Task<OutboxRunnerContext<TPayload>> LoopAsync(
            IReadOnlyCollection<Entities.OutboxMessage<TPayload>> messages,
            CancellationToken stoppingToken)
        {
            var context = new OutboxRunnerContext<TPayload>(_dateTimeProvider, messages, options);

            try
            {
                using var batchScope = _serviceProvider.CreateScope();

                var startTimestamp = Stopwatch.GetTimestamp();

                var localHandler = handler ?? batchScope.ServiceProvider.GetRequiredService<THandler>();

                await localHandler.HandleAsync(
                    messages: [.. messages.Select(c => MapMessage(c, context))],
                    cancellationToken: stoppingToken);

                var elapsed = Stopwatch.GetElapsedTime(startTimestamp);

                await _metricsHandler.HandledAsync(
                    identifier: identifier,
                    count: messages.Count,
                    elapsed: elapsed,
                    cancellationToken: stoppingToken);
            }
            catch (Exception ex) when (ex.IsCancel() && cancellationToken.IsCancellationRequested)
            {
                // Graceful shutdown
                throw;
            }
            catch (Exception ex)
            {
                var type = typeof(TPayload);

                _logger.ErrorProcessingMessages(type.FullName ?? type.Name, ex);

                context.Fail();
            }

            return context;
        }

        static OutboxMessage<TPayload> MapMessage(Entities.OutboxMessage<TPayload> message, OutboxRunnerContext<TPayload> context)
            => new(message, context);

        static Entities.OutboxMessage<TPayload> MapRetry(OutboxRunnerContext<TPayload>.RetryRequest retryRequest)
        {
            return new Entities.OutboxMessage<TPayload>(
                Id: retryRequest.Message.Id,
                Payload: retryRequest.Message.Payload,
                Attempt: retryRequest.Message.Attempt + 1,
                ScheduledAt: retryRequest.ScheduledAt,
                CreatedAt: retryRequest.Message.CreatedAt);
        }
    }
}
