using System;

namespace Yautbox.Infrastructure.DateTime;

internal interface IDateTimeProvider
{
    DateTimeOffset GetNow();
}
