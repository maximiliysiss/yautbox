namespace Yautbox.InMemory.Infrastructure;

internal sealed class DateTimeProvider : IDateTimeProvider
{
    public DateTimeOffset GetNow() => DateTimeOffset.UtcNow;
}
