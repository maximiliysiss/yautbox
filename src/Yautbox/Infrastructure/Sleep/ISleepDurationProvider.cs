using System;

namespace Yautbox.Infrastructure.Sleep;

internal interface ISleepDurationProvider
{
    TimeSpan GetSleepDelay(int attempt);
}
