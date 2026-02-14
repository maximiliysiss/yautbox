using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using NpgsqlTypes;
using Yautbox.Entities;
using Yautbox.Postgres.Extensions.Logger;
using Yautbox.Postgres.Extensions.Npgsql;
using Yautbox.Postgres.Infrastructure.Database;
using Yautbox.Postgres.Infrastructure.DateTime;
using Yautbox.Postgres.Options;
using Yautbox.Runner.Options;

namespace Yautbox.Postgres.Repositories;

internal sealed class PostgresOutboxRepository : IPostgresOutboxRepository
{
    private readonly ILogger<PostgresOutboxRepository> _logger;

    private readonly PostgresOutboxRepositoryOptions _options;

    private readonly IDateTimeProvider _dateTimeProvider;

    private readonly IOutboxConnectionFactory _connectionFactory;

    public PostgresOutboxRepository(
        ILogger<PostgresOutboxRepository> logger,
        IOptionsSnapshot<PostgresOutboxRepositoryOptions> options,
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
        var query = @$"
UPDATE {_options.SchemaName}.outbox_messages om
SET locker = :locker
FROM (
    SELECT id
    FROM {_options.SchemaName}.outbox_messages_active
    WHERE type = :type
      AND (scheduled_at IS NULL OR scheduled_at <= :now)
      AND (locker IS NULL OR locker <= :now)
    ORDER BY id
    LIMIT :count
    FOR UPDATE SKIP LOCKED
) s
WHERE om.id = s.id AND NOT om.is_deleted
RETURNING om.id, om.payload, om.attempt,
    om.scheduled_at AS scheduledAt, om.created_at AS createdAt;
";

        var now = _dateTimeProvider.GetNow();

        await using var connection = await _connectionFactory.GetConnectionAsync(cancellationToken);

        await using DbCommand command = new DbCommandInitializer(query, connection)
        {
            Parameters =
            {
                { "count", count },
                { "type", identifier },
                { "now", now },
                { "locker", now.Add(locker) }
            }
        };

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

        var query = @$"
INSERT INTO {_options.SchemaName}.outbox_messages(type, payload, created_at, attempt, scheduled_at, is_deleted)
SELECT :type, payload, created_at, attempt, scheduled_at, false
FROM unnest(:payloads, :attempts, :scheduled_ats, :created_ats) AS t(payload, attempt, scheduled_at, created_at)
RETURNING id;
";

        await using var connection = await _connectionFactory.GetConnectionAsync(cancellationToken);

        await using DbCommand command = new DbCommandInitializer(query, connection)
        {
            Parameters =
            {
                { "type", identifier },
                { "payloads", messages.Select(Map).ToArray(), NpgsqlDbType.Array | NpgsqlDbType.Jsonb },
                { "attempts", messages.Select(m => m.Attempt).ToArray() },
                { "scheduled_ats", messages.Select(m => m.ScheduledAt).ToArray() },
                { "created_ats", messages.Select(m => m.CreatedAt).ToArray() }
            }
        };

        await connection.OpenAsync(cancellationToken);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
            yield return new OutboxMessageId(reader.GetInt64("id"));

        yield break;

        string Map(OutboxMessage<T> m) => JsonSerializer.Serialize(m.Payload, _options.JsonSerializerOptions);
    }

    public async Task DeleteAsync(
        IReadOnlyCollection<OutboxMessageId> ids,
        DeletePolicy policy,
        CancellationToken cancellationToken)
    {
        if (ids.Count is 0)
            return;

        var safeDeleteQuery = @$"
UPDATE {_options.SchemaName}.outbox_messages
SET is_deleted = true,
    locker = NULL
WHERE id = ANY(:ids) AND NOT is_deleted;
";

        var fullDeleteQuery = $@"
DELETE FROM {_options.SchemaName}.outbox_messages
WHERE id = ANY(:ids) AND NOT is_deleted;
";

        var query = policy switch
        {
            DeletePolicy.Safe => safeDeleteQuery,
            DeletePolicy.Delete => fullDeleteQuery,
            _ => safeDeleteQuery,
        };

        await using var connection = await _connectionFactory.GetConnectionAsync(cancellationToken);

        await using DbCommand command = new DbCommandInitializer(query, connection)
        {
            Parameters =
            {
                { "ids", ids.Select(c => c.Value).ToArray() }
            }
        };

        await connection.OpenAsync(cancellationToken);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task UpdateAsync<T>(IReadOnlyCollection<OutboxMessage<T>> messages, CancellationToken cancellationToken)
    {
        if (messages.Count is 0)
            return;

        var query = @$"
UPDATE {_options.SchemaName}.outbox_messages om
SET attempt = t.attempt,
    scheduled_at = t.scheduled_at,
    locker = NULL
FROM unnest(:ids, :attempts, :scheduled_ats) AS t(id, attempt, scheduled_at)
WHERE om.id = t.id AND NOT is_deleted;
";

        await using var connection = await _connectionFactory.GetConnectionAsync(cancellationToken);

        await using DbCommand command = new DbCommandInitializer(query, connection)
        {
            Parameters =
            {
                { "ids", messages.Select(m => m.Id.Value).ToArray() },
                { "attempts", messages.Select(m => m.Attempt).ToArray() },
                { "scheduled_ats", messages.Select(m => m.ScheduledAt).ToArray() }
            }
        };

        await connection.OpenAsync(cancellationToken);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task CleanAsync(string identifier, DateTimeOffset olderThan, CancellationToken cancellationToken)
    {
        var query = $@"
WITH deleted AS (
    SELECT id
    FROM {_options.SchemaName}.outbox_messages_deleted
    WHERE type = :type AND created_at <= :olderThan
    LIMIT :limit
    FOR UPDATE SKIP LOCKED
)
DELETE FROM {_options.SchemaName}.outbox_messages om
WHERE om.id IN (SELECT id FROM deleted);
";

        while (!cancellationToken.IsCancellationRequested)
        {
            await using var connection = await _connectionFactory.GetConnectionAsync(cancellationToken);

            await using DbCommand command = new DbCommandInitializer(query, connection)
            {
                Parameters =
                {
                    { "type", identifier },
                    { "olderThan", olderThan },
                    { "limit", _options.CleanupBatchSize }
                }
            };

            await connection.OpenAsync(cancellationToken);

            var rowAffected = await command.ExecuteNonQueryAsync(cancellationToken);

            if (rowAffected is 0)
                break;
        }
    }
}
