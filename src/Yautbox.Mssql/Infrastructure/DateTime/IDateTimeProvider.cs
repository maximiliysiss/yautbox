using System;

namespace Yautbox.Mssql.Infrastructure.DateTime;

internal interface IDateTimeProvider
{
    DateTimeOffset GetNow();
}
