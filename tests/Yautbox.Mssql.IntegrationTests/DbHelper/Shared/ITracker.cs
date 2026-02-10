namespace Yautbox.Mssql.IntegrationTests.DbHelper.Shared;

public interface ITracker<T>
{
    T Track(T entity);
}
