using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Yautbox.Entities;
using Yautbox.Mysql.Extensions.Logger;
using Yautbox.Mysql.Extensions.MySql;
using Yautbox.Mysql.Infrastructure.Database;
using Yautbox.Mysql.Infrastructure.DateTime;
using Yautbox.Mysql.Options;
using Yautbox.Mysql.Transactions;
using Yautbox.Runner.Options;

namespace Yautbox.Mysql.Repositories;

internal sealed class MysqlOutboxRepository : IMysqlOutboxRepository
{
    private readonly ILogger<MysqlOutboxRepository> _logger;
    private readonly MysqlOutboxRepositoryOptions _options;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IOutboxConnectionFactory _connectionFactory;

    public MysqlOutboxRepository(
        ILogger<MysqlOutboxRepository> logger,
        IOptionsSnapshot<MysqlOutboxRepositoryOptions> options,
        IDateTimeProvider dateTimeProvider,
        IOutboxConnectionFactory connectionFactory)
    {
        _logger = logger;
        _dateTimeProvider = dateTimeProvider;
        _connectionFactory = connectionFactory;
        _options = options.Value;
    }

    public async IAsyncEnumerable<OutboxMessage<T>> GetAsync<T>(
        string identifier,
        int count,
        TimeSpan locker,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var lockedBy = Guid.NewGuid().ToString("N");
        var tableName = $"`{_options.SchemaName}`.`outbox_messages`";

        var selectQuery = $@"
START TRANSACTION;

UPDATE {tableName} t
JOIN (
    SELECT id
    FROM {tableName}
    WHERE type = @type
      AND is_deleted = 0
      AND (scheduled_at IS NULL OR scheduled_at <= @now)
      AND (locker IS NULL OR locker <= @now)
    ORDER BY id
    LIMIT @count
    FOR UPDATE SKIP LOCKED
) s ON s.id = t.id
SET t.locker = @locker,
    t.locked_by = @lockedBy;

SELECT id, payload, attempt, scheduled_at, created_at
FROM outbox_messages
WHERE locked_by = @lockedBy;

COMMIT;
";

        var now = _dateTimeProvider.GetNow();
        var nowUtc = now.UtcDateTime;
        var lockerUtc = now.Add(locker).UtcDateTime;

        await using var connection = await _connectionFactory.GetConnectionAsync(cancellationToken);
        await connection.OpenAsync(cancellationToken);

        await using DbCommand selectCommand = connection.CreateCommand();
        selectCommand.CommandText = selectQuery;

        selectCommand.Parameters.Add("type", identifier);
        selectCommand.Parameters.Add("now", nowUtc);
        selectCommand.Parameters.Add("count", count);
        selectCommand.Parameters.Add("locker", lockerUtc);
        selectCommand.Parameters.Add("lockedBy", lockedBy);

        await using var reader = await selectCommand.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            var payloadString = reader.GetNullableString("payload");

            if (string.IsNullOrWhiteSpace(payloadString))
            {
                _logger.OutboxPayloadInvalid(identifier);
                continue;
            }

            var payload = JsonSerializer.Deserialize<T>(
                json: payloadString,
                options: _options.JsonSerializerOptions);

            if (payload is null)
            {
                _logger.OutboxPayloadInvalid(identifier);
                continue;
            }

            var id = reader.GetInt64(reader.GetOrdinal("id"));
            var scheduledAt = reader.GetNullableDateTime("scheduledAt");
            var createdAt = reader.GetDateTime(reader.GetOrdinal("createdAt"));

            yield return new OutboxMessage<T>(
                Id: new OutboxMessageId(id),
                Payload: payload,
                Attempt: reader.GetInt32(reader.GetOrdinal("attempt")),
                ScheduledAt: scheduledAt.HasValue ? ToDateTimeOffsetUtc(scheduledAt.Value) : null,
                CreatedAt: ToDateTimeOffsetUtc(createdAt));
        }
    }

    public async IAsyncEnumerable<OutboxMessageId> AddAsync<T>(
        string identifier,
        IReadOnlyCollection<OutboxMessage<T>> messages,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (messages.Count is 0)
            yield break;

        var tableName = $"`{_options.SchemaName}`.`outbox_messages`";

        var query = $@"
INSERT INTO {tableName} (type, payload, created_at, attempt, scheduled_at, is_deleted)
VALUES (@type, @payload, @created_at, @attempt, @scheduled_at, 0);
";

        await using var connection = await _connectionFactory.GetConnectionAsync(cancellationToken);
        await connection.OpenAsync(cancellationToken);

        await using var transaction = Transaction.Current() is null
            ? await connection.BeginTransactionAsync(cancellationToken)
            : null;

        await using DbCommand command = connection.CreateCommand();
        command.CommandText = query;
        command.Transaction = transaction;

        command.Parameters.Add("type", identifier);
        command.Parameters.Add("payload", string.Empty);
        command.Parameters.Add("created_at", DateTime.UtcNow);
        command.Parameters.Add("attempt", 0);
        command.Parameters.Add("scheduled_at", DBNull.Value);

        await using DbCommand idCommand = connection.CreateCommand();
        idCommand.CommandText = "SELECT LAST_INSERT_ID();";
        idCommand.Transaction = transaction;

        foreach (var message in messages)
        {
            command.Parameters["payload"].Value = JsonSerializer.Serialize(message.Payload, _options.JsonSerializerOptions);
            command.Parameters["created_at"].Value = ToUtcDateTime(message.CreatedAt);
            command.Parameters["attempt"].Value = message.Attempt;
            command.Parameters["scheduled_at"].Value = ToDbValue(message.ScheduledAt);

            await command.ExecuteNonQueryAsync(cancellationToken);

            var idValue = await idCommand.ExecuteScalarAsync(cancellationToken);
            var id = Convert.ToInt64(idValue, CultureInfo.InvariantCulture);

            yield return new OutboxMessageId(id);
        }

        await (transaction?.CommitAsync(cancellationToken) ?? Task.CompletedTask);
    }

    public async Task DeleteAsync(
        IReadOnlyCollection<OutboxMessageId> ids,
        DeletePolicy policy,
        CancellationToken cancellationToken)
    {
        if (ids.Count is 0)
            return;

        var tableName = $"`{_options.SchemaName}`.`outbox_messages`";

        var safeDeleteQuery = $@"
UPDATE {tableName}
SET is_deleted = 1,
    locker = NULL,
    locked_by = NULL
WHERE id IN ({{0}}) AND is_deleted = 0;
";

        var fullDeleteQuery = $@"
DELETE FROM {tableName}
WHERE id IN ({{0}}) AND is_deleted = 0;
";

        var template = policy switch
        {
            DeletePolicy.Safe => safeDeleteQuery,
            DeletePolicy.Delete => fullDeleteQuery,
            _ => safeDeleteQuery,
        };

        await using var connection = await _connectionFactory.GetConnectionAsync(cancellationToken);

        await using DbCommand command = connection.CreateCommand();
        var idsClause = BuildIdParameters(command, ids, "id");
        command.CommandText = string.Format(CultureInfo.InvariantCulture, template, idsClause);

        await connection.OpenAsync(cancellationToken);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task UpdateAsync<T>(IReadOnlyCollection<OutboxMessage<T>> messages, CancellationToken cancellationToken)
    {
        if (messages.Count is 0)
            return;

        var tableName = $"`{_options.SchemaName}`.`outbox_messages`";

        var query = $@"
UPDATE {tableName}
SET attempt = @attempt,
    scheduled_at = @scheduled_at,
    locker = NULL,
    locked_by = NULL
WHERE id = @id AND is_deleted = 0;
";

        await using var connection = await _connectionFactory.GetConnectionAsync(cancellationToken);
        await connection.OpenAsync(cancellationToken);

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await using DbCommand command = connection.CreateCommand();
        command.CommandText = query;
        command.Transaction = transaction;

        command.Parameters.Add("id", 0L);
        command.Parameters.Add("attempt", 0);
        command.Parameters.Add("scheduled_at", DBNull.Value);

        foreach (var message in messages)
        {
            command.Parameters["id"].Value = message.Id.Value;
            command.Parameters["attempt"].Value = message.Attempt;
            command.Parameters["scheduled_at"].Value = ToDbValue(message.ScheduledAt);

            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    public async Task CleanAsync(string identifier, DateTimeOffset olderThan, CancellationToken cancellationToken)
    {
        var tableName = $"`{_options.SchemaName}`.`outbox_messages`";

        var query = $@"
DELETE FROM {tableName}
WHERE is_deleted = 1
  AND type = @type
  AND created_at <= @olderThan
LIMIT @limit;
";

        while (!cancellationToken.IsCancellationRequested)
        {
            await using var connection = await _connectionFactory.GetConnectionAsync(cancellationToken);

            await using DbCommand command = connection.CreateCommand();
            command.CommandText = query;
            command.Parameters.Add("type", identifier);
            command.Parameters.Add("olderThan", ToUtcDateTime(olderThan));
            command.Parameters.Add("limit", _options.CleanupBatchSize);

            await connection.OpenAsync(cancellationToken);

            var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);

            if (rowsAffected is 0)
                break;
        }
    }

    private static string BuildIdParameters(DbCommand command, IReadOnlyCollection<OutboxMessageId> ids, string prefix)
    {
        var parameters = new string[ids.Count];
        var index = 0;

        foreach (var id in ids)
        {
            var name = $"{prefix}{index}";
            command.Parameters.Add(name, id.Value);
            parameters[index] = $"@{name}";
            index++;
        }

        return string.Join(", ", parameters);
    }

    private static string BuildIdParameters(DbCommand command, IReadOnlyCollection<long> ids, string prefix)
    {
        var parameters = new string[ids.Count];
        var index = 0;

        foreach (var id in ids)
        {
            var name = $"{prefix}{index}";
            command.Parameters.Add(name, id);
            parameters[index] = $"@{name}";
            index++;
        }

        return string.Join(", ", parameters);
    }

    private static DateTimeOffset ToDateTimeOffsetUtc(DateTime value)
    {
        var utc = DateTime.SpecifyKind(value, DateTimeKind.Utc);
        return new DateTimeOffset(utc);
    }

    private static DateTime ToUtcDateTime(DateTimeOffset value) => value.UtcDateTime;

    private static object ToDbValue(DateTimeOffset? value)
        => value.HasValue ? value.Value.UtcDateTime : DBNull.Value;
}
