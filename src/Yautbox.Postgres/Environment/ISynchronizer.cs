namespace Yautbox.Postgres.Environment;

internal interface ISynchronizer
{
    Task ReadyAsync(CancellationToken cancellationToken);
}
