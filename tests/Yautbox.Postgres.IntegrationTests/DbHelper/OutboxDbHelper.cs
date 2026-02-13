using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using AutoBogus;
using Bogus;
using Microsoft.Extensions.Options;
using Npgsql;
using NpgsqlTypes;
using Yautbox.Entities;
using Yautbox.Postgres.Infrastructure.Database;
using Yautbox.Postgres.IntegrationTests.DbHelper.Shared;
using Yautbox.Postgres.IntegrationTests.Shared.Extensions;
using Yautbox.Postgres.Options;

namespace Yautbox.Postgres.IntegrationTests.DbHelper;

internal sealed class OutboxDbHelper : IDbHelper, ITracker<OutboxMessageId>
{
    private readonly IOutboxConnectionFactory _connectionFactory;

    private readonly HashSet<long> _ids = [];

    private readonly string _options;

    public OutboxDbHelper(IOutboxConnectionFactory connectionFactory, IOptions<PostgresOutboxRepositoryOptions> options)
    {
        _connectionFactory = connectionFactory;
        _options = options.Value.SchemaName;
    }

    public async IAsyncEnumerable<TableRow> GetAsync<T>(string? identifier = null, params long[] ids)
    {
        identifier ??= $"{RuntimeInformation.FrameworkDescription}_{typeof(T).GetVersionFreeFullName()}";

        var query = @$"
SELECT id, type, payload, created_at AS createdAt, attempt, scheduled_at AS scheduledAt, is_deleted AS isDeleted, locker
FROM {_options}.outbox_messages
WHERE (cardinality(:ids) = 0 OR id = ANY(:ids)) AND type = :type
";

        await using var connection = await _connectionFactory.GetConnectionAsync(CancellationToken.None);

        await using DbCommand command = new DbCommandInitializer(query, connection)
        {
            Parameters =
            {
                { "ids", ids },
                { "type", identifier },
            }
        };

        await connection.OpenAsync(CancellationToken.None);

        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            yield return new TableRow
            {
                Attempt = reader.GetInt32("attempt"),
                CreatedAt = reader.GetFieldValue<DateTimeOffset>("createdAt"),
                Id = reader.GetInt64("id"),
                IsDeleted = reader.GetBoolean("isDeleted"),
                Payload = reader.GetString("payload"),
                ScheduledAt = reader.GetFieldValue<DateTimeOffset?>("scheduledAt"),
                Type = reader.GetString("type"),
            };
        }
    }

    public async Task<long> AddAsync(TableRow row)
    {
        var query = @$"
INSERT INTO {_options}.outbox_messages(type, payload, created_at, attempt, scheduled_at, is_deleted, locker)
VALUES (:type, :payload, :createdAt, :attempt, :scheduledAt, :isDeleted, :locker)
RETURNING id
";

        await using var connection = await _connectionFactory.GetConnectionAsync(CancellationToken.None);

        await using DbCommand command = new DbCommandInitializer(query, connection)
        {
            Parameters =
            {
                { "type", row.Type },
                { "payload", row.Payload, NpgsqlDbType.Jsonb },
                { "createdAt", row.CreatedAt },
                { "attempt", row.Attempt },
                { "scheduledAt", row.ScheduledAt },
                { "isDeleted", row.IsDeleted },
                { "locker", row.Locker },
            }
        };

        await connection.OpenAsync(CancellationToken.None);

        var id = (long?)await command.ExecuteScalarAsync() ?? throw new InvalidOperationException("No id returned.");

        _ids.Add(id);
        row.Id = id;

        return id;
    }

    public async Task DeleteAsync(params long[] ids)
    {
        var query = @$"
DELETE FROM {_options}.outbox_messages
WHERE id = ANY(:ids)
";

        await using var connection = await _connectionFactory.GetConnectionAsync(CancellationToken.None);

        await using DbCommand command = new DbCommandInitializer(query, connection)
        {
            Parameters =
            {
                { "ids", ids },
            }
        };

        await connection.OpenAsync(CancellationToken.None);

        await command.ExecuteNonQueryAsync();
    }

    public ValueTask DisposeAsync() => new(DeleteAsync([.. _ids]));

    public OutboxMessageId Track(OutboxMessageId entity)
    {
        _ids.Add(entity.Value);
        return entity;
    }

    public sealed class TableRow
    {
        public static Faker<TableRow> GetFaker(string type, string? payload = null) => new AutoFaker<TableRow>()
            .RuleFor(c => c.Type, type)
            .RuleFor(c => c.Payload, payload ?? "{}")
            .RuleFor(c => c.CreatedAt, DateTimeOffset.UtcNow)
            .RuleFor(c => c.ScheduledAt, (DateTimeOffset?)null)
            .RuleFor(c => c.Attempt, 0)
            .RuleFor(c => c.Locker, (DateTimeOffset?)null)
            .RuleFor(c => c.IsDeleted, false);

        public static TableRow GetDefault(string type, string? payload = null) => GetFaker(type, payload).Generate();

        public long Id { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Payload { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; set; }
        public int Attempt { get; set; }
        public DateTimeOffset? ScheduledAt { get; set; }
        public bool IsDeleted { get; set; }
        public DateTimeOffset? Locker { get; set; }
    }
}
