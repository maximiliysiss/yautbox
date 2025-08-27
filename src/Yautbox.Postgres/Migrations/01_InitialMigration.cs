using System;
using FluentMigrator;
using Microsoft.Extensions.Options;
using Yautbox.Postgres.Migrations.Options;

namespace Yautbox.Postgres.Migrations;

[Migration(1, "InitialMigration")]
public sealed class InitialMigration : SqlMigration
{
    private readonly MigrationOptions _options;

    public InitialMigration(IOptions<MigrationOptions> options) => _options = options.Value;

    protected override string GetUpSql(IServiceProvider services) =>
        $"""
        CREATE TABLE {_options.SchemaName}.outbox_messages
        (
            message_id      BIGINT GENERATED ALWAYS AS IDENTITY,
            message_type    TEXT                     NOT NULL,
            payload         jsonb                    NOT NULL,
            created_at      TIMESTAMP WITH TIME ZONE NOT NULL,
            PRIMARY KEY (message_id)
        );
        """;

    protected override string GetDownSql(IServiceProvider services) =>
        $"""
        DROP TABLE {_options.SchemaName}.outbox_messages;
        """;
}
