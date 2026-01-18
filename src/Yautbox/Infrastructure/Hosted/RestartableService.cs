using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using Yautbox.Extensions.DateTime;
using Yautbox.Extensions.Logger;
using Yautbox.Infrastructure.Sleep;

namespace Yautbox.Infrastructure.Hosted;

internal abstract class RestartableService : BackgroundService
{
    private readonly AsyncRetryPolicy _retryPolicy;

    private readonly ILogger _logger;

    protected virtual string ServiceName => GetType().Name;

    protected RestartableService(ILogger logger, ISleepDurationProvider? sleepDurationProvider = null)
    {
        _logger = logger;

        var delayProvider = sleepDurationProvider ?? DefaultSleepDurationProvider.Instance;

        _retryPolicy = Policy
            .Handle<Exception>(e => e is not OperationCanceledException)
            .WaitAndRetryForeverAsync(
                delayProvider.GetSleepDelay,
                (e, retryNumber, _) => _logger.RestartingService(ServiceName, retryNumber, e));
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken) => _retryPolicy.ExecuteAsync(ExecuteLoop, stoppingToken);

    protected abstract Task ExecuteAsync(CancellationTokenSource reloadTokenSource);

    private async Task ExecuteLoop(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var reloadTokenSource = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);

            try
            {
                _logger.StartingService(ServiceName);
                await ExecuteAsync(reloadTokenSource);
            }
            catch (OperationCanceledException)
            {
                if (stoppingToken.IsCancellationRequested)
                    throw;

                if (reloadTokenSource.IsCancellationRequested)
                    _logger.ConfigurationChanged(ServiceName);
            }
            catch (Exception)
            {
                await reloadTokenSource.CancelAsync();
                throw;
            }
        }
    }

    private sealed class DefaultSleepDurationProvider : ISleepDurationProvider
    {
        public static readonly DefaultSleepDurationProvider Instance = new();

        public TimeSpan GetSleepDelay(int attempt)
        {
            var power = Math.Min(attempt, 3);
            return TimeSpan.FromSeconds(Math.Pow(2, power)).Jitter();
        }
    }
}
