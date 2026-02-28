using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MySqlConnector;
using Yautbox.Entities;
using Yautbox.Mysql.Extensions.DateTime;
using Yautbox.Mysql.Extensions.Logger;
using Yautbox.Mysql.Repositories;
using Yautbox.Provider;
using Yautbox.Runner.Options;

namespace Yautbox.Mysql.Provider;

internal sealed class MysqlOutboxProvider : IOutboxProvider
{
    private readonly IMysqlOutboxRepository _repository;

    private readonly MysqlOutboxProviderOptions _options;

    private readonly ILogger<MysqlOutboxProvider> _logger;

    public MysqlOutboxProvider(
        IMysqlOutboxRepository repository,
        IOptions<MysqlOutboxProviderOptions> options,
        ILogger<MysqlOutboxProvider> logger)
    {
        _repository = repository;
        _logger = logger;
        _options = options.Value;
    }

    public async Task<IReadOnlyCollection<OutboxMessageId>> AddAsync<T>(
        string identifier,
        IReadOnlyCollection<OutboxMessage<T>> messages,
        CancellationToken cancellationToken)
    {
        for (var i = 0; i < _options.DeadlockRetryCount; i++)
        {
            try
            {
                return await _repository
                    .AddAsync(identifier, messages, cancellationToken)
                    .ToArrayAsync(cancellationToken);
            }
            catch (MySqlException ex) when (ex.ErrorCode is MySqlErrorCode.LockWaitTimeout or MySqlErrorCode.LockDeadlock)
            {
                var delay = _options.DeadlockDelay.Jitter();
                _logger.DeadlockRetryDelay(delay);
                await Task.Delay(delay, cancellationToken);
            }
        }

        _logger.DeadlockDetected();
        throw new InvalidOperationException("Failed to add messages to outbox because of deadlock.");
    }

    public async Task<IReadOnlyCollection<OutboxMessage<T>>> GetAsync<T>(
        string identifier,
        int count,
        TimeSpan visibility,
        CancellationToken cancellationToken)
    {
        return await _repository
            .GetAsync<T>(identifier, count, visibility, cancellationToken)
            .ToArrayAsync(cancellationToken);
    }

    public Task CancelAsync(
        string identifier,
        IReadOnlyCollection<OutboxMessageId> ids,
        DeletePolicy policy,
        CancellationToken cancellationToken)
        => DeleteAsync(identifier, ids, policy, cancellationToken);

    public Task DeleteAsync(
        string identifier,
        IReadOnlyCollection<OutboxMessageId> ids,
        DeletePolicy policy,
        CancellationToken cancellationToken)
        => _repository.DeleteAsync(ids, policy, cancellationToken);

    public Task CleanAsync(
        string identifier,
        DateTimeOffset olderThan,
        CancellationToken cancellationToken)
        => _repository.CleanAsync(identifier, olderThan, cancellationToken);

    public Task RetryAsync<T>(
        string identifier,
        IReadOnlyCollection<OutboxMessage<T>> messages,
        CancellationToken cancellationToken)
        => _repository.UpdateAsync(messages, cancellationToken);
}
