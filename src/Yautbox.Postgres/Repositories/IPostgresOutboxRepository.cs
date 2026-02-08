using Yautbox.Entities;
using Yautbox.Runner.Options;

namespace Yautbox.Postgres.Repositories;

internal interface IPostgresOutboxRepository
{
    IAsyncEnumerable<OutboxMessage<T>> GetAsync<T>(
        string identifier,
        int count,
        TimeSpan locker,
        OutboxExecutionPolicy policy,
        CancellationToken cancellationToken);

    IAsyncEnumerable<OutboxMessageId> AddAsync<T>(
        string identifier,
        IReadOnlyCollection<OutboxMessage<T>> messages,
        CancellationToken cancellationToken);

    Task DeleteAsync(
        IReadOnlyCollection<OutboxMessageId> ids,
        DeletePolicy policy,
        CancellationToken cancellationToken);

    Task UpdateAsync<T>(
        IReadOnlyCollection<OutboxMessage<T>> messages,
        CancellationToken cancellationToken);

    Task CleanAsync(
        string identifier,
        DateTimeOffset olderThan,
        CancellationToken cancellationToken);
}
