namespace Yautbox.Mysql.IntegrationTests.DbHelper.Shared;

public interface ITracker<T>
{
    T Track(T entity);
}
