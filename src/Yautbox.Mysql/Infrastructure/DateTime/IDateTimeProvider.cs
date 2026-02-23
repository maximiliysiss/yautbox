using System;

namespace Yautbox.Mysql.Infrastructure.DateTime;

internal interface IDateTimeProvider
{
    DateTimeOffset GetNow();
}
