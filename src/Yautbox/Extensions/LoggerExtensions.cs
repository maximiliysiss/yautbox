namespace Yautbox.Extensions;

internal static partial class LoggerExtensions
{
    [LoggerMessage(1, LogLevel.Debug, "Added messages to outbox")]
    public static partial void AddedOutboxMessage(this ILogger logger);

    [LoggerMessage(2, LogLevel.Warning, "Stopped outbox service {Name}.")]
    public static partial void StoppedOutbox(this ILogger logger, string name);
}
