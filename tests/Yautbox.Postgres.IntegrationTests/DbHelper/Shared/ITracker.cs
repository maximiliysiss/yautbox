namespace Yautbox.Postgres.IntegrationTests.DbHelper.Shared;

public interface ITracker<T>
{
    T Track(T entity);
}
