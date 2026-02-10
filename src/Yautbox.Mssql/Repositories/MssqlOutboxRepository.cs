using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Yautbox.Entities;
using Yautbox.Mssql.Extensions.Logger;
using Yautbox.Mssql.Extensions.SqlClient;
using Yautbox.Mssql.Infrastructure.Database;
using Yautbox.Mssql.Infrastructure.DateTime;
using Yautbox.Mssql.Options;
using Yautbox.Runner.Options;

namespace Yautbox.Mssql.Repositories;

internal sealed class MssqlOutboxRepository : IMssqlOutboxRepository
{
    private readonly ILogger<MssqlOutboxRepository> _logger;
    private readonly MssqlOutboxRepositoryOptions _options;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IOutboxConnectionFactory _connectionFactory;

    public MssqlOutboxRepository(
        ILogger<MssqlOutboxRepository> logger,
        IOptionsSnapshot<MssqlOutboxRepositoryOptions> options,
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
        var tableName = $"[{_options.SchemaName}].outbox_messages";

        var query = $@"
WITH cte AS (
    SELECT TOP (@count)
        id,
        payload,
        attempt,
        scheduled_at,
        created_at,
        locker
    FROM {tableName} WITH (ROWLOCK, READPAST, UPDLOCK)
    WHERE type = @type
      AND is_deleted = 0
      AND (scheduled_at IS NULL OR scheduled_at <= @now)
      AND (locker IS NULL OR locker <= @now)
    ORDER BY id
)
UPDATE cte
SET locker = @locker
OUTPUT inserted.id, inserted.payload, inserted.attempt,
       inserted.scheduled_at AS scheduledAt, inserted.created_at AS createdAt;
";

        var now = _dateTimeProvider.GetNow();

        await using var connection = await _connectionFactory.GetConnectionAsync(cancellationToken);

        await using DbCommand command = connection.CreateCommand();
        command.CommandText = query;
        command.Parameters.Add("count", count);
        command.Parameters.Add("type", identifier);
        command.Parameters.Add("now", now);
        command.Parameters.Add("locker", now.Add(locker));

        await connection.OpenAsync(cancellationToken);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

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

            yield return new OutboxMessage<T>(
                Id: new OutboxMessageId(reader.GetInt64("id")),
                Payload: payload,
                Attempt: reader.GetInt32("attempt"),
                ScheduledAt: reader.GetFieldValue<DateTimeOffset?>("scheduledAt"),
                CreatedAt: reader.GetFieldValue<DateTimeOffset>("createdAt"));
        }
    }

    public async IAsyncEnumerable<OutboxMessageId> AddAsync<T>(
        string identifier,
        IReadOnlyCollection<OutboxMessage<T>> messages,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (messages.Count is 0)
            yield break;

        var tableName = $"[{_options.SchemaName}].outbox_messages";
        var typeName = $"{_options.SchemaName}.outbox_message_table_type";

        var query = $@"
INSERT INTO {tableName} (type, payload, created_at, attempt, scheduled_at, is_deleted)
OUTPUT inserted.id
SELECT @type, payload, created_at, attempt, scheduled_at, 0
FROM @messages;
";

        await using var connection = await _connectionFactory.GetConnectionAsync(cancellationToken);

        await using DbCommand command = connection.CreateCommand();
        command.CommandText = query;
        command.Parameters.Add("type", identifier);
        command.Parameters.AddTable("messages", CreateMessageTable(messages), typeName);

        await connection.OpenAsync(cancellationToken);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
            yield return new OutboxMessageId(reader.GetInt64("id"));
    }

    public async Task DeleteAsync(
        IReadOnlyCollection<OutboxMessageId> ids,
        DeletePolicy policy,
        CancellationToken cancellationToken)
    {
        if (ids.Count is 0)
            return;

        var tableName = $"[{_options.SchemaName}].outbox_messages";
        var typeName = $"{_options.SchemaName}.outbox_id_table_type";

        var safeDeleteQuery = $@"
UPDATE {tableName}
SET is_deleted = 1,
    locker = NULL
WHERE id IN (SELECT id FROM @ids) AND is_deleted = 0;
";

        var fullDeleteQuery = $@"
DELETE FROM {tableName}
WHERE id IN (SELECT id FROM @ids) AND is_deleted = 0;
";

        var query = policy switch
        {
            DeletePolicy.Safe => safeDeleteQuery,
            DeletePolicy.Delete => fullDeleteQuery,
            _ => safeDeleteQuery,
        };

        await using var connection = await _connectionFactory.GetConnectionAsync(cancellationToken);

        await using DbCommand command = connection.CreateCommand();
        command.CommandText = query;
        command.Parameters.AddTable("ids", CreateIdTable(ids), typeName);

        await connection.OpenAsync(cancellationToken);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task UpdateAsync<T>(IReadOnlyCollection<OutboxMessage<T>> messages, CancellationToken cancellationToken)
    {
        if (messages.Count is 0)
            return;

        var tableName = $"[{_options.SchemaName}].outbox_messages";
        var typeName = $"{_options.SchemaName}.outbox_update_table_type";

        var query = $@"
UPDATE om
SET attempt = u.attempt,
    scheduled_at = u.scheduled_at,
    locker = NULL
FROM {tableName} om
INNER JOIN @updates u ON om.id = u.id
WHERE om.is_deleted = 0;
";

        await using var connection = await _connectionFactory.GetConnectionAsync(cancellationToken);

        await using DbCommand command = connection.CreateCommand();
        command.CommandText = query;
        command.Parameters.AddTable("updates", CreateUpdateTable(messages), typeName);

        await connection.OpenAsync(cancellationToken);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task CleanAsync(string identifier, DateTimeOffset olderThan, CancellationToken cancellationToken)
    {
        var tableName = $"[{_options.SchemaName}].outbox_messages";

        var query = $@"
DELETE TOP (@limit)
FROM {tableName}
WHERE is_deleted = 1
  AND type = @type
  AND created_at <= @olderThan;
";

        while (!cancellationToken.IsCancellationRequested)
        {
            await using var connection = await _connectionFactory.GetConnectionAsync(cancellationToken);

            await using DbCommand command = connection.CreateCommand();
            command.CommandText = query;
            command.Parameters.Add("type", identifier);
            command.Parameters.Add("olderThan", olderThan);
            command.Parameters.Add("limit", _options.CleanupBatchSize);

            await connection.OpenAsync(cancellationToken);

            var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);

            if (rowsAffected is 0)
                break;
        }
    }

    private DataTable CreateMessageTable<T>(IReadOnlyCollection<OutboxMessage<T>> messages)
    {
        var table = new DataTable();

        var payloadColumn = table.Columns.Add("payload", typeof(string));
        payloadColumn.AllowDBNull = false;

        var attemptColumn = table.Columns.Add("attempt", typeof(int));
        attemptColumn.AllowDBNull = false;

        var scheduledAtColumn = table.Columns.Add("scheduled_at", typeof(DateTimeOffset));
        scheduledAtColumn.AllowDBNull = true;

        var createdAtColumn = table.Columns.Add("created_at", typeof(DateTimeOffset));
        createdAtColumn.AllowDBNull = false;

        foreach (var message in messages)
        {
            var row = table.NewRow();
            row["payload"] = JsonSerializer.Serialize(message.Payload, _options.JsonSerializerOptions);
            row["attempt"] = message.Attempt;
            row["scheduled_at"] = message.ScheduledAt ?? (object)DBNull.Value;
            row["created_at"] = message.CreatedAt;
            table.Rows.Add(row);
        }

        return table;
    }

    private static DataTable CreateIdTable(IReadOnlyCollection<OutboxMessageId> ids)
    {
        var table = new DataTable();

        var idColumn = table.Columns.Add("id", typeof(long));
        idColumn.AllowDBNull = false;

        foreach (var id in ids)
        {
            var row = table.NewRow();
            row["id"] = id.Value;
            table.Rows.Add(row);
        }

        return table;
    }

    private static DataTable CreateUpdateTable<T>(IReadOnlyCollection<OutboxMessage<T>> messages)
    {
        var table = new DataTable();

        var idColumn = table.Columns.Add("id", typeof(long));
        idColumn.AllowDBNull = false;

        var attemptColumn = table.Columns.Add("attempt", typeof(int));
        attemptColumn.AllowDBNull = false;

        var scheduledAtColumn = table.Columns.Add("scheduled_at", typeof(DateTimeOffset));
        scheduledAtColumn.AllowDBNull = true;

        foreach (var message in messages)
        {
            var row = table.NewRow();
            row["id"] = message.Id.Value;
            row["attempt"] = message.Attempt;
            row["scheduled_at"] = message.ScheduledAt ?? (object)DBNull.Value;
            table.Rows.Add(row);
        }

        return table;
    }
}
