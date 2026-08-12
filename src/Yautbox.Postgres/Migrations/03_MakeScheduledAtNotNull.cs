using System;
using FluentMigrator;
using Microsoft.Extensions.Options;
using Yautbox.Postgres.Migrations.Options;
using Yautbox.Postgres.Migrations.Shared;

namespace Yautbox.Postgres.Migrations;

[Migration(3, "MakeScheduledAtNotNull")]
internal sealed class MakeScheduledAtNotNull : SqlMigration
{
    private readonly MigrationOptions _options;

    public MakeScheduledAtNotNull(IOptions<MigrationOptions> options) => _options = options.Value;

    protected override string GetUpSql(IServiceProvider services)
    {
        return $"""
                UPDATE {_options.SchemaName}.outbox_messages
                SET scheduled_at = '-infinity'
                WHERE scheduled_at IS NULL;

                ALTER TABLE {_options.SchemaName}.outbox_messages
                    ALTER COLUMN scheduled_at SET DEFAULT '-infinity',
                    ALTER COLUMN scheduled_at SET NOT NULL;
                """;
    }

    protected override string GetDownSql(IServiceProvider services)
    {
        return $"""
                ALTER TABLE {_options.SchemaName}.outbox_messages
                    ALTER COLUMN scheduled_at DROP DEFAULT,
                    ALTER COLUMN scheduled_at DROP NOT NULL;

                UPDATE {_options.SchemaName}.outbox_messages
                SET scheduled_at = null
                WHERE scheduled_at = '-infinity';
                """;
    }
}
