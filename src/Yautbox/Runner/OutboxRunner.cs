using System;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Yautbox.Handlers;
using Yautbox.Infrastructure;
using Yautbox.Persistence;

namespace Yautbox.Runner;

internal class OutboxRunner<THandler, TPayload, TOptions> : RestartableService
    where THandler : IOutboxHandler<TPayload>
    where TOptions : IOutboxRunnerOptions
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IInfrastructureReadinessWaiter _readinessWaiter;

    private readonly IOptionsMonitor<TOptions> _options;

    public OutboxRunner(
        IPlatformLifecycle lifecycle,
        IServiceProvider serviceProvider,
        IOptionsMonitor<TOptions> options,
        IInfrastructureReadinessWaiter readinessWaiter,
        ILogger<OutboxRunner<THandler, TPayload, TOptions>> logger)
        : base(logger, lifecycle)
    {
        _serviceProvider = serviceProvider;
        _readinessWaiter = readinessWaiter;
        _options = options;
    }

    protected override string ServiceName => $"{GetType().Name}[{typeof(TPayload).Name},{typeof(THandler).Name}]";

    protected override async Task ExecuteAsync(CancellationTokenSource reloadTokenSource)
    {
        using var optionsRegistration = _options.OnChange(_ => reloadTokenSource.Cancel());
        var cancellationToken = reloadTokenSource.Token;

        await _readinessWaiter.WaitAsync(cancellationToken);

        var options = _options.CurrentValue;

        if (options.IsDisabled)
        {
            Logger.StoppedOutbox(ServiceName);

            await Task.Delay(Timeout.Infinite, cancellationToken);
        }

        using var scope = _serviceProvider.CreateScope();

        var repository = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
        var handler = scope.ServiceProvider.GetRequiredService<THandler>();

        while (!cancellationToken.IsCancellationRequested)
        {
            await ProcessingLoopAsync(repository, handler, cancellationToken);

            await Task.Delay(options.PollDelay, cancellationToken);
        }
    }

    private async Task ProcessingLoopAsync(
        IOutboxRepository repository,
        IOutboxHandler<TPayload> handler,
        CancellationToken cancellationToken)
    {
        bool succeededProcessing;
        do
        {
            using var handleCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            handleCancellation.CancelAfter(_options.CurrentValue.HandleTimeout);

            succeededProcessing = await TryProcessingMessagesAsync(repository, handler, handleCancellation.Token);
        }
        while (succeededProcessing && !cancellationToken.IsCancellationRequested);
    }

    private async Task<bool> TryProcessingMessagesAsync(
        IOutboxRepository repository,
        IOutboxHandler<TPayload> handler,
        CancellationToken cancellationToken)
    {
        using var scope = new TransactionScope(
            TransactionScopeOption.Required,
            new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted },
            TransactionScopeAsyncFlowOption.Enabled);

        var messages = await repository
            .ListAsync<TPayload>(_options.CurrentValue.BufferSize, cancellationToken)
            .ToArrayAsync(cancellationToken);

        if (messages is [])
            return false;

        await handler.HandleAsync(messages.Select(m => m.Payload), cancellationToken);
        await repository.DeleteAsync(
            messageIds: messages.Select(m => m.Id),
            cancellationToken);

        scope.Complete();

        return true;
    }
}
