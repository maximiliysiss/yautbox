using System;

namespace Yautbox.Mssql.Infrastructure.DateTime;

internal sealed class DateTimeProvider : IDateTimeProvider
{
    public DateTimeOffset GetNow() => DateTimeOffset.UtcNow;
}
