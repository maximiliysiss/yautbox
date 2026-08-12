using System;
using FluentMigrator;
using Microsoft.Extensions.Options;
using Yautbox.Postgres.Migrations.Options;
using Yautbox.Postgres.Migrations.Shared;

namespace Yautbox.Postgres.Migrations;

[Migration(4, TransactionBehavior.None, "AddIndex")]
internal sealed class AddIndex : SqlMigration
{
    private readonly MigrationOptions _options;

    public AddIndex(IOptions<MigrationOptions> options) => _options = options.Value;

    protected override string GetUpSql(IServiceProvider services)
    {
        return $"""
                CREATE INDEX CONCURRENTLY idx__outbox_messages_active__type_id_scheduled_at
                    ON {_options.SchemaName}.outbox_messages_active (type, id, scheduled_at);
                """;
    }

    protected override string GetDownSql(IServiceProvider services)
    {
        return $"""
                DROP INDEX CONCURRENTLY {_options.SchemaName}.idx__outbox_messages_active__type_id_scheduled_at;
                """;
    }
}
