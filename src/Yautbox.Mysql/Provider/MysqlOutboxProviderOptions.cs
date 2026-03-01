using System;

namespace Yautbox.Mysql.Provider;

internal sealed class MysqlOutboxProviderOptions
{
    public int DeadlockRetryCount { get; set; } = 3;
    public TimeSpan DeadlockDelay { get; set; } = TimeSpan.FromMilliseconds(50);
}
