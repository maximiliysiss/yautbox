using System;

namespace Yautbox.Infrastructure.DateTime;

internal sealed class DateTimeProvider : IDateTimeProvider
{
    public DateTimeOffset GetNow() => DateTimeOffset.UtcNow;
}
