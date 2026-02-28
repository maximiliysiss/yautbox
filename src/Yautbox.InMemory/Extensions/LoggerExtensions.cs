using System;
using Microsoft.Extensions.Logging;
using Yautbox.Entities;
using Yautbox.Runner.Options;

namespace Yautbox.InMemory.Extensions;

internal static partial class LoggerExtensions
{
    [LoggerMessage(1, LogLevel.Warning, "Rescheduling message with id = '{Id}' from outbox '{Name}'.")]
    public static partial void ReschedulingMessage(this ILogger logger, OutboxMessageId id, string name);

    [LoggerMessage(2, LogLevel.Debug, "Fetching up to {count} outbox message(s) for {identifier} with visibility {visibility}.")]
    public static partial void FetchingOutboxMessages(this ILogger logger, string identifier, int count, TimeSpan visibility);

    [LoggerMessage(3, LogLevel.Debug, "Fetched {count} outbox message(s) for {identifier}.")]
    public static partial void FetchedOutboxMessages(this ILogger logger, string identifier, int count);

    [LoggerMessage(4, LogLevel.Debug, "Adding {count} outbox message(s) for {identifier}.")]
    public static partial void AddingOutboxMessages(this ILogger logger, string identifier, int count);

    [LoggerMessage(5, LogLevel.Debug, "Added {count} outbox message(s) for {identifier}.")]
    public static partial void AddedOutboxMessages(this ILogger logger, string identifier, int count);

    [LoggerMessage(6, LogLevel.Debug, "Deleting {count} outbox message(s) for {identifier} using {policy}.")]
    public static partial void DeletingOutboxMessages(this ILogger logger, string identifier, int count, DeletePolicy policy);

    [LoggerMessage(7, LogLevel.Debug, "Retrying {count} outbox message(s) for {identifier}.")]
    public static partial void RetryingOutboxMessages(this ILogger logger, string identifier, int count);

    [LoggerMessage(8, LogLevel.Debug, "Cleaning outbox {identifier} older than {olderThan}.")]
    public static partial void CleaningOutboxMessages(this ILogger logger, string identifier, DateTimeOffset olderThan);
}
