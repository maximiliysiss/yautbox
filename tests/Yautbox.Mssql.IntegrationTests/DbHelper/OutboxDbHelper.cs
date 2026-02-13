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
using Yautbox.Entities;
using Yautbox.Mssql.Extensions.SqlClient;
using Yautbox.Mssql.Infrastructure.Database;
using Yautbox.Mssql.IntegrationTests.DbHelper.Shared;
using Yautbox.Mssql.IntegrationTests.Shared.Extensions;
using Yautbox.Mssql.Options;

namespace Yautbox.Mssql.IntegrationTests.DbHelper;

internal sealed class OutboxDbHelper : IDbHelper, ITracker<OutboxMessageId>
{
    private readonly IOutboxConnectionFactory _connectionFactory;
    private readonly HashSet<long> _ids = [];
    private readonly string _schema;

    public OutboxDbHelper(IOutboxConnectionFactory connectionFactory, IOptions<MssqlOutboxRepositoryOptions> options)
    {
        _connectionFactory = connectionFactory;
        _schema = options.Value.SchemaName;
    }

    public async IAsyncEnumerable<TableRow> GetAsync<T>(string? identifier = null, params long[] ids)
    {
        identifier ??= $"{RuntimeInformation.FrameworkDescription}_{typeof(T).GetVersionFreeFullName()}";

        var tableName = $"[{_schema}].outbox_messages";
        var idTypeName = $"{_schema}.outbox_id_table_type";

        var query = $@"
SELECT id, type, payload, created_at AS createdAt, attempt, scheduled_at AS scheduledAt, is_deleted AS isDeleted, locker
FROM {tableName}
WHERE (@idsCount = 0 OR id IN (SELECT id FROM @ids)) AND type = @type;
";

        await using var connection = await _connectionFactory.GetConnectionAsync(CancellationToken.None);

        await using DbCommand command = connection.CreateCommand();
        command.CommandText = query;
        command.Parameters.Add("type", identifier);
        command.Parameters.Add("idsCount", ids.Length);
        command.Parameters.AddTable("ids", CreateIdTable(ids), idTypeName);

        await connection.OpenAsync(CancellationToken.None);

        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            yield return new TableRow
            {
                Attempt = reader.GetInt32(reader.GetOrdinal("attempt")),
                CreatedAt = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("createdAt")),
                Id = reader.GetInt64(reader.GetOrdinal("id")),
                IsDeleted = reader.GetBoolean(reader.GetOrdinal("isDeleted")),
                Payload = reader.GetString(reader.GetOrdinal("payload")),
                ScheduledAt = reader.GetNullableFieldValue<DateTimeOffset?>("scheduledAt"),
                Type = reader.GetString(reader.GetOrdinal("type")),
                Locker = reader.IsDBNull(reader.GetOrdinal("locker"))
                    ? null
                    : reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("locker"))
            };
        }
    }

    public async Task<long> AddAsync(TableRow row)
    {
        var tableName = $"[{_schema}].outbox_messages";

        var query = $@"
INSERT INTO {tableName}(type, payload, created_at, attempt, scheduled_at, is_deleted, locker)
OUTPUT inserted.id
VALUES (@type, @payload, @createdAt, @attempt, @scheduledAt, @isDeleted, @locker);
";

        await using var connection = await _connectionFactory.GetConnectionAsync(CancellationToken.None);

        await using DbCommand command = connection.CreateCommand();
        command.CommandText = query;
        command.Parameters.Add("type", row.Type);
        command.Parameters.Add("payload", row.Payload);
        command.Parameters.Add("createdAt", row.CreatedAt);
        command.Parameters.Add("attempt", row.Attempt);
        command.Parameters.Add("scheduledAt", row.ScheduledAt ?? (object)DBNull.Value);
        command.Parameters.Add("isDeleted", row.IsDeleted);
        command.Parameters.Add("locker", row.Locker ?? (object)DBNull.Value);

        await connection.OpenAsync(CancellationToken.None);

        var id = (long?)await command.ExecuteScalarAsync() ?? throw new InvalidOperationException("No id returned.");

        _ids.Add(id);
        row.Id = id;

        return id;
    }

    public async Task DeleteAsync(params long[] ids)
    {
        if (ids.Length is 0)
            return;

        var tableName = $"[{_schema}].outbox_messages";
        var idTypeName = $"{_schema}.outbox_id_table_type";

        var query = $@"
DELETE FROM {tableName}
WHERE id IN (SELECT id FROM @ids);
";

        await using var connection = await _connectionFactory.GetConnectionAsync(CancellationToken.None);

        await using DbCommand command = connection.CreateCommand();
        command.CommandText = query;
        command.Parameters.AddTable("ids", CreateIdTable(ids), idTypeName);

        await connection.OpenAsync(CancellationToken.None);

        await command.ExecuteNonQueryAsync();
    }

    public ValueTask DisposeAsync() => new(DeleteAsync([.. _ids]));

    public OutboxMessageId Track(OutboxMessageId entity)
    {
        _ids.Add(entity.Value);
        return entity;
    }

    private static DataTable CreateIdTable(IEnumerable<long> ids)
    {
        var table = new DataTable();

        var idColumn = table.Columns.Add("id", typeof(long));
        idColumn.AllowDBNull = false;

        foreach (var id in ids)
        {
            var row = table.NewRow();
            row["id"] = id;
            table.Rows.Add(row);
        }

        return table;
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
