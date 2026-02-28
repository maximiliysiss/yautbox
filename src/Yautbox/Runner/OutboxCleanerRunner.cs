using System;
using System.Diagnostics;
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
using Yautbox.Runner.Options;

namespace Yautbox.Runner;

internal sealed class OutboxCleanerRunner<THandler, TPayload> : RestartableService where THandler : IOutboxHandler<TPayload>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IInfrastructureReadinessWaiter _readinessWaiter;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IMetricsHandler _metricsHandler;

    private readonly ILogger<OutboxCleanerRunner<THandler, TPayload>> _logger;

    private readonly IOptionsMonitor<IOutboxRunnerOptions> _options;

    public OutboxCleanerRunner(
        ILogger<OutboxCleanerRunner<THandler, TPayload>> logger,
        IOptionsMonitor<IOutboxRunnerOptions> options,
        IServiceProvider serviceProvider,
        IInfrastructureReadinessWaiter readinessWaiter,
        IDateTimeProvider dateTimeProvider,
        IMetricsHandler metricsHandler) : base(logger)
    {
        _logger = logger;
        _options = options;
        _serviceProvider = serviceProvider;
        _readinessWaiter = readinessWaiter;
        _dateTimeProvider = dateTimeProvider;
        _metricsHandler = metricsHandler;

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
            { BackupInterval: null } => () => _logger.OutboxCleanupIsDisabled(ServiceName),
            _ when options.Validate() is ValidationResult.FailureValidationResult r
                => () => _logger.InvalidOutboxOptions(ServiceName, r.ErrorMessage),
            _ => null,
        };

        if (options.BackupInterval is null)
        {
            _logger.OutboxCleanupIsDisabled(ServiceName);
            await Task.Delay(Timeout.Infinite, cancellationToken);
            return;
        }

        if (invalidAction is not null)
        {
            invalidAction();
            await Task.Delay(Timeout.Infinite, cancellationToken);
            return;
        }

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();

                var provider = scope.ServiceProvider.GetRequiredService<IOutboxProvider>();
                var registry = scope.ServiceProvider.GetRequiredService<IOutboxRegistry>();

                var olderThan = _dateTimeProvider.GetNow() - options.BackupInterval.Value;
                var identifier = registry.GetIdentifier<TPayload>();

                _logger.OutboxCleanupStarted(identifier, olderThan);

                var startTimestamp = Stopwatch.GetTimestamp();

                await provider.CleanAsync(identifier, olderThan, cancellationToken);

                var elapsed = Stopwatch.GetElapsedTime(startTimestamp);

                await _metricsHandler.CleanedInAsync(identifier, elapsed, cancellationToken);

                _logger.OutboxCleanupFinished(identifier, elapsed);

                await Task.Delay(options.BackupInterval.Value, cancellationToken);
            }
            catch (Exception ex) when (ex.IsCancel() && cancellationToken.IsCancellationRequested)
            {
                // Graceful shutdown
                return;
            }
            catch (Exception ex)
            {
                var type = typeof(TPayload);

                _logger.ErrorOutboxBackgroundService(type.FullName ?? type.Name, ex);

                // Prevent tight loop on errors
                await Task.Delay(options.FailureDelay.Jitter(), cancellationToken);
            }
        }
    }
}
