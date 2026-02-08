using System;

namespace Yautbox.InMemory.Infrastructure;

internal interface IDateTimeProvider
{
    DateTimeOffset GetNow();
}
