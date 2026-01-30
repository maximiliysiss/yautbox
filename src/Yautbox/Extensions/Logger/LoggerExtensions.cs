using System;
using Microsoft.Extensions.Logging;

namespace Yautbox.Extensions.Logger;

internal static partial class LoggerExtensions
{
    [LoggerMessage(1, LogLevel.Debug, "Added messages to outbox")]
    public static partial void AddedOutboxMessage(this ILogger logger);

    [LoggerMessage(2, LogLevel.Warning, "Outbox '{Name}' is disabled.")]
    public static partial void DisableOutbox(this ILogger logger, string name);

    [LoggerMessage(3, LogLevel.Information, "Starting service {hostedService}.")]
    public static partial void StartingService(this ILogger logger, string hostedService);

    [LoggerMessage(4, LogLevel.Error, "Restarting service {hostedService} due to an exception. Retry #{retryNumber}.")]
    public static partial void RestartingService(this ILogger logger, string hostedService, int retryNumber, Exception exception);

    [LoggerMessage(5, LogLevel.Information, "Restarting service {hostedService} due to a configuration change.")]
    public static partial void ConfigurationChanged(this ILogger logger, string hostedService);

    [LoggerMessage(6, LogLevel.Debug, "Cancel outbox messages")]
    public static partial void CancelOutboxMessage(this ILogger logger);

    [LoggerMessage(7, LogLevel.Error, "Error in outbox background service loop for message type {type}")]
    public static partial void ErrorOutboxBackgroundService(this ILogger logger, string type, Exception exception);

    [LoggerMessage(8, LogLevel.Error, "Error processing outbox messages of type {type}")]
    public static partial void ErrorProcessingMessages(this ILogger logger, string type, Exception exception);

    [LoggerMessage(9, LogLevel.Information, "Outbox cleanup is disabled for service {serviceName}.")]
    public static partial void OutboxCleanupIsDisabled(this ILogger logger, string serviceName);

    [LoggerMessage(10, LogLevel.Warning, "Invalid time to live interval for service {serviceName}: {interval}.")]
    public static partial void InvalidTimeToLiveInterval(this ILogger logger, string serviceName, TimeSpan interval);
}
