using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using AutoBogus;
using Bogus;
using Microsoft.Extensions.Options;
using Yautbox.Entities;
using Yautbox.Mysql.Infrastructure.Database;
using Yautbox.Mysql.IntegrationTests.DbHelper.Shared;
using Yautbox.Mysql.IntegrationTests.Shared.Extensions;
using Yautbox.Mysql.Options;
using Yautbox.Mysql.Extensions.MySql;

namespace Yautbox.Mysql.IntegrationTests.DbHelper;

internal sealed class OutboxDbHelper : IDbHelper, ITracker<OutboxMessageId>
{
    private readonly IOutboxConnectionFactory _connectionFactory;
    private readonly HashSet<long> _ids = [];
    private readonly string _schema;

    public OutboxDbHelper(IOutboxConnectionFactory connectionFactory, IOptions<MysqlOutboxRepositoryOptions> options)
    {
        _connectionFactory = connectionFactory;
        _schema = options.Value.SchemaName;
    }

    public async IAsyncEnumerable<TableRow> GetAsync<T>(string? identifier = null, params long[] ids)
    {
        identifier ??= $"{RuntimeInformation.FrameworkDescription}_{typeof(T).GetVersionFreeFullName()}";

        var tableName = $"`{_schema}`.`outbox_messages`";

        await using var connection = await _connectionFactory.GetConnectionAsync(CancellationToken.None);
        await connection.OpenAsync(CancellationToken.None);

        await using DbCommand command = connection.CreateCommand();

        // Собираем WHERE под ids (если ids пустой — не добавляем фильтр)
        var idsFilter = string.Empty;
        if (ids is { Length: > 0 })
        {
            var idsClause = BuildIdParameters(command, ids, "id");
            idsFilter = $" AND id IN ({idsClause})";
        }

        command.CommandText = $@"
SELECT id, type, payload,
       created_at AS createdAt,
       attempt,
       scheduled_at AS scheduledAt,
       is_deleted AS isDeleted,
       locker
FROM {tableName}
WHERE type = @type{idsFilter};
";

        command.Parameters.Add("type", identifier);

        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var createdAt = reader.GetDateTime(reader.GetOrdinal("createdAt"));
            var scheduledAt = reader.GetNullableDateTime("scheduledAt");
            var lockerAt = reader.GetNullableDateTime("locker");

            yield return new TableRow
            {
                Attempt = reader.GetInt32(reader.GetOrdinal("attempt")),
                CreatedAt = ToDateTimeOffsetUtc(createdAt),
                Id = reader.GetInt64(reader.GetOrdinal("id")),
                IsDeleted = reader.GetBoolean(reader.GetOrdinal("isDeleted")),
                Payload = reader.GetString(reader.GetOrdinal("payload")),
                ScheduledAt = scheduledAt.HasValue ? ToDateTimeOffsetUtc(scheduledAt.Value) : null,
                Type = reader.GetString(reader.GetOrdinal("type")),
                Locker = lockerAt.HasValue ? ToDateTimeOffsetUtc(lockerAt.Value) : null
            };
        }
    }

    public async Task<long> AddAsync(TableRow row)
    {
        var tableName = $"`{_schema}`.`outbox_messages`";

        var insertQuery = $@"
INSERT INTO {tableName} (type, payload, created_at, attempt, scheduled_at, is_deleted, locker)
VALUES (@type, @payload, @createdAt, @attempt, @scheduledAt, @isDeleted, @locker);
";

        await using var connection = await _connectionFactory.GetConnectionAsync(CancellationToken.None);
        await connection.OpenAsync(CancellationToken.None);

        await using var transaction = await connection.BeginTransactionAsync(CancellationToken.None);

        await using (DbCommand command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = insertQuery;

            command.Parameters.Add("type", row.Type);
            command.Parameters.Add("payload", row.Payload);
            command.Parameters.Add("createdAt", row.CreatedAt.UtcDateTime);
            command.Parameters.Add("attempt", row.Attempt);
            command.Parameters.Add<object>("scheduledAt", row.ScheduledAt.HasValue ? row.ScheduledAt.Value.UtcDateTime : DBNull.Value);
            command.Parameters.Add("isDeleted", row.IsDeleted);
            command.Parameters.Add<object>("locker", row.Locker.HasValue ? row.Locker.Value.UtcDateTime : DBNull.Value);

            await command.ExecuteNonQueryAsync();
        }

        long id;
        await using (DbCommand idCommand = connection.CreateCommand())
        {
            idCommand.Transaction = transaction;
            idCommand.CommandText = "SELECT LAST_INSERT_ID();";

            var idValue = await idCommand.ExecuteScalarAsync();
            id = Convert.ToInt64(idValue, CultureInfo.InvariantCulture);
        }

        await transaction.CommitAsync(CancellationToken.None);

        _ids.Add(id);
        row.Id = id;

        return id;
    }

    public async Task DeleteAsync(params long[] ids)
    {
        if (ids.Length is 0)
            return;

        var tableName = $"`{_schema}`.`outbox_messages`";

        await using var connection = await _connectionFactory.GetConnectionAsync(CancellationToken.None);
        await connection.OpenAsync(CancellationToken.None);

        await using DbCommand command = connection.CreateCommand();

        var idsClause = BuildIdParameters(command, ids, "id");

        command.CommandText = $@"
DELETE FROM {tableName}
WHERE id IN ({idsClause});
";

        await command.ExecuteNonQueryAsync();
    }

    public ValueTask DisposeAsync() => new(DeleteAsync([.. _ids]));

    public OutboxMessageId Track(OutboxMessageId entity)
    {
        _ids.Add(entity.Value);
        return entity;
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
