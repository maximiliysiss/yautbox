using Microsoft.Extensions.Logging;
using Yautbox.Entities;

namespace Yautbox.InMemory.Extensions;

internal static partial class LoggerExtensions
{
    [LoggerMessage(1, LogLevel.Warning, "Rescheduling message with id = '{Id}' from outbox '{Name}'.")]
    public static partial void ReschedulingMessage(this ILogger logger, OutboxMessageId id, string name);
}
