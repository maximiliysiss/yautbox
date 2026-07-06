using System;
using Microsoft.Extensions.Logging;

namespace Yautbox.Extensions.Logger;

internal static partial class LoggerExtensions
{
    [LoggerMessage(1, LogLevel.Debug, "Adding {count} message(s) to outbox {identifier}. ScheduledAt: {scheduledAt}")]
    public static partial void AddedOutboxMessage(this ILogger logger, string identifier, int count, DateTimeOffset? scheduledAt);

    [LoggerMessage(2, LogLevel.Warning, "Outbox '{Name}' is disabled.")]
    public static partial void DisableOutbox(this ILogger logger, string name);

    [LoggerMessage(3, LogLevel.Information, "Starting service {hostedService}.")]
    public static partial void StartingService(this ILogger logger, string hostedService);

    [LoggerMessage(4, LogLevel.Error, "Restarting service {hostedService} due to an exception. Retry #{retryNumber}.")]
    public static partial void RestartingService(this ILogger logger, string hostedService, int retryNumber, Exception exception);

    [LoggerMessage(5, LogLevel.Information, "Restarting service {hostedService} due to a configuration change.")]
    public static partial void ConfigurationChanged(this ILogger logger, string hostedService);

    [LoggerMessage(6, LogLevel.Debug, "Canceling {count} outbox message(s) for {identifier}.")]
    public static partial void CancelOutboxMessage(this ILogger logger, string identifier, int count);

    [LoggerMessage(7, LogLevel.Error, "Error in outbox background service loop for message type {type}")]
    public static partial void ErrorOutboxBackgroundService(this ILogger logger, string type, Exception exception);

    [LoggerMessage(8, LogLevel.Error, "Error processing outbox messages of type {type}")]
    public static partial void ErrorProcessingMessages(this ILogger logger, string type, Exception exception);

    [LoggerMessage(9, LogLevel.Information, "Outbox cleanup is disabled for service {serviceName}.")]
    public static partial void OutboxCleanupIsDisabled(this ILogger logger, string serviceName);

    [LoggerMessage(10, LogLevel.Warning, "Invalid time to live interval for service {serviceName}: {interval}.")]
    public static partial void InvalidTimeToLiveInterval(this ILogger logger, string serviceName, TimeSpan interval);

    [LoggerMessage(11, LogLevel.Error, "Invalid outbox options for service {serviceName}: {errorMessage}")]
    public static partial void InvalidOutboxOptions(this ILogger logger, string serviceName, string errorMessage);

    [LoggerMessage(12, LogLevel.Information, "Stopping service {serviceName}.")]
    public static partial void StoppingService(this ILogger logger, string serviceName);

    [LoggerMessage(13, LogLevel.Debug, "Fetched {count} outbox message(s) for {identifier} in {elapsed}.")]
    public static partial void OutboxMessagesFetched(this ILogger logger, string identifier, int count, TimeSpan elapsed);

    [LoggerMessage(14, LogLevel.Debug, "Processed outbox messages for {identifier}: fetched={fetchedCount}, retry={retryCount}, delete={deleteCount}.")]
    public static partial void OutboxMessagesProcessed(this ILogger logger, string identifier, int fetchedCount, int retryCount, int deleteCount);

    [LoggerMessage(15, LogLevel.Debug, "Starting outbox cleanup for {identifier}. OlderThan={olderThan}.")]
    public static partial void OutboxCleanupStarted(this ILogger logger, string identifier, DateTimeOffset olderThan);

    [LoggerMessage(16, LogLevel.Debug, "Finished outbox cleanup for {identifier} in {elapsed}.")]
    public static partial void OutboxCleanupFinished(this ILogger logger, string identifier, TimeSpan elapsed);

    [LoggerMessage(17, LogLevel.Information, "Outbox retrying {count} message(s) for {identifier} because of restarting.")]
    public static partial void OutboxRetrying(this ILogger logger, string identifier, int count);
}
