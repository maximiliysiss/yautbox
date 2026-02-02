namespace Yautbox.Postgres.Infrastructure.DateTime;

internal interface IDateTimeProvider
{
    DateTimeOffset GetNow();
}
