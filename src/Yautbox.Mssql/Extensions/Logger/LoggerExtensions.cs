using Microsoft.Extensions.Logging;

namespace Yautbox.Mssql.Extensions.Logger;

internal static partial class LoggerExtensions
{
    [LoggerMessage(1, LogLevel.Warning, "Outbox payload is invalid and cannot be deserialized into {identifier}")]
    public static partial void OutboxPayloadInvalid(this ILogger logger, string identifier);
}
