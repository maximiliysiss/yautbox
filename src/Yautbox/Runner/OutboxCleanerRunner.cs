using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Yautbox.Extensions.DateTime;
using Yautbox.Extensions.Logger;
using Yautbox.Handlers;
using Yautbox.Infrastructure.DateTime;
using Yautbox.Infrastructure.Hosted;
using Yautbox.Infrastructure.Waiter;
using Yautbox.Provider;
using Yautbox.Runner.Options;

namespace Yautbox.Runner;

internal sealed class OutboxCleanerRunner<THandler, TPayload> : RestartableService where THandler : IOutboxHandler<TPayload>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IInfrastructureReadinessWaiter _readinessWaiter;
    private readonly IDateTimeProvider _dateTimeProvider;

    private readonly ILogger<OutboxCleanerRunner<THandler, TPayload>> _logger;

    private readonly IOptionsMonitor<IOutboxRunnerOptions> _options;

    public OutboxCleanerRunner(
        ILogger<OutboxCleanerRunner<THandler, TPayload>> logger,
        IOptionsMonitor<IOutboxRunnerOptions> options,
        IServiceProvider serviceProvider,
        IInfrastructureReadinessWaiter readinessWaiter,
        IDateTimeProvider dateTimeProvider) : base(logger)
    {
        _logger = logger;
        _options = options;
        _serviceProvider = serviceProvider;
        _readinessWaiter = readinessWaiter;
        _dateTimeProvider = dateTimeProvider;
    }

    protected override async Task ExecuteAsync(CancellationTokenSource reloadTokenSource)
    {
        using var optionsRegistration = _options.OnChange(_ => reloadTokenSource.Cancel());
        var cancellationToken = reloadTokenSource.Token;

        await _readinessWaiter.WaitAsync(cancellationToken);

        var options = _options.CurrentValue;

        Action? invalidMessage = options switch
        {
            { IsEnabled: false } => () => _logger.DisableOutbox(ServiceName),
            { BackupInterval: null } => () => _logger.OutboxCleanupIsDisabled(ServiceName),
            var o when o.BackupInterval <= TimeSpan.Zero => () => _logger.InvalidTimeToLiveInterval(ServiceName, o.BackupInterval.Value),
            _ => null,
        };

        if (options.BackupInterval is null)
        {
            _logger.OutboxCleanupIsDisabled(ServiceName);
            await Task.Delay(Timeout.Infinite, cancellationToken);
            return;
        }

        if (invalidMessage is not null)
        {
            invalidMessage();
            await Task.Delay(Timeout.Infinite, cancellationToken);
            return;
        }

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();

                var provider = scope.ServiceProvider.GetRequiredService<IOutboxProvider>();

                var olderThan = _dateTimeProvider.GetNow() - options.BackupInterval.Value;
                await provider.CleanAsync(olderThan, cancellationToken);

                await Task.Delay(options.BackupInterval.Value, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Graceful shutdown
                return;
            }
            catch (Exception ex)
            {
                _logger.ErrorOutboxBackgroundService(typeof(TPayload).Name, ex);

                // Prevent tight loop on errors
                await Task.Delay(options.FailureDelay.Jitter(), cancellationToken);
            }
        }
    }
}
