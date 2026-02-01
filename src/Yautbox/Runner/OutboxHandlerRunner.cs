using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Yautbox.Extensions.DateTime;
using Yautbox.Extensions.Enumerable;
using Yautbox.Extensions.Logger;
using Yautbox.Extensions.Options;
using Yautbox.Handlers;
using Yautbox.Infrastructure.DateTime;
using Yautbox.Infrastructure.Hosted;
using Yautbox.Infrastructure.Waiter;
using Yautbox.Provider;
using Yautbox.Registy;
using Yautbox.Runner.Options;

namespace Yautbox.Runner;

internal class OutboxHandlerRunner<THandler, TPayload> : RestartableService where THandler : IOutboxHandler<TPayload>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IInfrastructureReadinessWaiter _readinessWaiter;
    private readonly IDateTimeProvider _dateTimeProvider;

    private readonly IOptionsMonitor<IOutboxRunnerOptions> _options;

    private readonly ILogger<OutboxHandlerRunner<THandler, TPayload>> _logger;

    public OutboxHandlerRunner(
        IServiceProvider serviceProvider,
        IOptionsMonitor<IOutboxRunnerOptions> options,
        IInfrastructureReadinessWaiter readinessWaiter,
        ILogger<OutboxHandlerRunner<THandler, TPayload>> logger,
        IDateTimeProvider dateTimeProvider)
        : base(logger)
    {
        _serviceProvider = serviceProvider;
        _readinessWaiter = readinessWaiter;
        _logger = logger;
        _dateTimeProvider = dateTimeProvider;
        _options = options;
    }

    protected override string ServiceName => $"{base.ServiceName}[{typeof(TPayload).Name},{typeof(THandler).Name}]";

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
                    using var scope = _serviceProvider.CreateScope();

                    var provider = scope.ServiceProvider.GetRequiredService<IOutboxProvider>();
                    var handler = scope.ServiceProvider.GetRequiredService<THandler>();

                    await HandleLoopAsync(identifier, provider, handler, options, stoppingToken);
                    await Task.Delay(options.PollDelay.Jitter(), stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    // Graceful shutdown
                    return;
                }
                catch (Exception ex)
                {
                    _logger.ErrorOutboxBackgroundService(typeof(TPayload).Name, ex);

                    // Prevent tight loop on errors
                    await Task.Delay(options.FailureDelay.Jitter(), stoppingToken);
                }
            }
        }
    }

    private async Task HandleLoopAsync(
        string identifier,
        IOutboxProvider provider,
        THandler handler,
        IOutboxRunnerOptions options,
        CancellationToken cancellationToken)
    {
        bool succeededProcessing;
        do succeededProcessing = await TryProcessingMessagesAsync(identifier, provider, handler, options, cancellationToken);
        while (succeededProcessing && !cancellationToken.IsCancellationRequested);
    }

    private async Task<bool> TryProcessingMessagesAsync(
        string identifier,
        IOutboxProvider provider,
        THandler handler,
        IOutboxRunnerOptions options,
        CancellationToken cancellationToken)
    {
        using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cancellationTokenSource.CancelAfter(options.HandleTimeout);

        var stoppingToken = cancellationTokenSource.Token;

        var loopTasks = await provider
            .GetAsync<TPayload>(identifier, options.BufferSize, options.Visibility, options.ExecutionPolicy, stoppingToken)
            .ChunkAsync(options.PerBufferCount, stoppingToken)
            .Select(g => LoopAsync(g, stoppingToken))
            .ToArrayAsync(stoppingToken);

        if (loopTasks is [])
            return false;

        var contexts = await Task.WhenAll(loopTasks);

        await provider.RetryAsync(
            identifier: identifier,
            messages: [.. contexts.SelectMany(c => c.Retries).Select(MapRetry)],
            cancellationToken: stoppingToken);

        await provider.DeleteAsync(
            ids: [.. contexts.SelectMany(c => c.Success)],
            policy: options.DeletePolicy,
            cancellationToken: stoppingToken);

        return contexts.Any(c => c.IsSuccess);

        async Task<OutboxRunnerContext<TPayload>> LoopAsync(
            IReadOnlyCollection<Entities.OutboxMessage<TPayload>> messages,
            CancellationToken stoppingToken)
        {
            var context = new OutboxRunnerContext<TPayload>(_dateTimeProvider, messages);

            try
            {
                await handler.HandleAsync(
                    messages: [.. messages.Select(c => MapMessage(c, context))],
                    cancellationToken: stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.ErrorProcessingMessages(typeof(TPayload).Name, ex);
                context.Fail(options.FailureDelay);
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
