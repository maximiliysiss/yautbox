using System;
using Microsoft.Extensions.Logging;
using Yautbox.Runner.Options;

namespace Yautbox.Mssql.Extensions.Logger;

internal static partial class LoggerExtensions
{
    [LoggerMessage(1, LogLevel.Warning, "Outbox payload is invalid and cannot be deserialized into {identifier}")]
    public static partial void OutboxPayloadInvalid(this ILogger logger, string identifier);

    [LoggerMessage(2, LogLevel.Debug, "Fetching up to {count} outbox message(s) for {identifier}.")]
    public static partial void FetchingOutboxMessages(this ILogger logger, string identifier, int count);

    [LoggerMessage(3, LogLevel.Debug, "Fetched {count} outbox message(s) for {identifier}.")]
    public static partial void FetchedOutboxMessages(this ILogger logger, string identifier, int count);

    [LoggerMessage(4, LogLevel.Debug, "Adding {count} outbox message(s) for {identifier}.")]
    public static partial void AddingOutboxMessages(this ILogger logger, string identifier, int count);

    [LoggerMessage(5, LogLevel.Debug, "Added {count} outbox message(s) for {identifier}.")]
    public static partial void AddedOutboxMessages(this ILogger logger, string identifier, int count);

    [LoggerMessage(6, LogLevel.Debug, "Deleting {requestedCount} outbox message(s) using {policy}.")]
    public static partial void DeletingOutboxMessages(this ILogger logger, int requestedCount, DeletePolicy policy);

    [LoggerMessage(7, LogLevel.Debug, "Deleted {rowsAffected} outbox message(s) (requested {requestedCount}) using {policy}.")]
    public static partial void DeletedOutboxMessages(this ILogger logger, int requestedCount, int rowsAffected, DeletePolicy policy);

    [LoggerMessage(8, LogLevel.Debug, "Updating {requestedCount} outbox message(s).")]
    public static partial void UpdatingOutboxMessages(this ILogger logger, int requestedCount);

    [LoggerMessage(9, LogLevel.Debug, "Updated {rowsAffected} outbox message(s) (requested {requestedCount}).")]
    public static partial void UpdatedOutboxMessages(this ILogger logger, int requestedCount, int rowsAffected);

    [LoggerMessage(10, LogLevel.Debug, "Cleaning outbox {identifier} older than {olderThan}.")]
    public static partial void CleaningOutboxMessages(this ILogger logger, string identifier, DateTimeOffset olderThan);

    [LoggerMessage(11, LogLevel.Debug, "Cleaned {rowsAffected} outbox message(s) for {identifier}.")]
    public static partial void CleanedOutboxMessages(this ILogger logger, string identifier, int rowsAffected);
}
