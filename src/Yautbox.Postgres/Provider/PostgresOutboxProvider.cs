using Microsoft.Extensions.Options;
using Yautbox.Entities;
using Yautbox.Postgres.Repositories;
using Yautbox.Provider;
using Yautbox.Runner.Options;

namespace Yautbox.Postgres.Provider;

internal sealed class PostgresOutboxProvider : IOutboxProvider
{
    private readonly IPostgresOutboxRepository _repository;

    private readonly PostgresOutboxProviderOptions _options;

    public PostgresOutboxProvider(IPostgresOutboxRepository repository, IOptionsSnapshot<PostgresOutboxProviderOptions> options)
    {
        _repository = repository;
        _options = options.Value;
    }

    public Task<IReadOnlyCollection<OutboxMessageId>> AddAsync<T>(
        string identifier,
        IReadOnlyCollection<OutboxMessage<T>> messages,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyCollection<OutboxMessage<T>>> GetAsync<T>(
        string identifier,
        int count,
        TimeSpan visibility,
        OutboxExecutionPolicy policy,
        CancellationToken cancellationToken)
    {
        _repository.GetAsync<T>(identifier, count, visibility, policy, cancellationToken);
    }

    public Task CancelAsync(IReadOnlyCollection<OutboxMessageId> ids, CancellationToken cancellationToken)
        => DeleteAsync(ids, _options.CancellingPolicy, cancellationToken);

    public Task DeleteAsync(
        IReadOnlyCollection<OutboxMessageId> ids,
        OutboxDeletePolicy policy,
        CancellationToken cancellationToken)
        => _repository.DeleteAsync(ids, policy, cancellationToken);

    public Task CleanAsync(
        string identifier,
        DateTimeOffset olderThan,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task RetryAsync<T>(
        string identifier,
        IReadOnlyCollection<OutboxMessage<T>> messages,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
