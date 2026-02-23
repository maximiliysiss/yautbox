using System;
using FluentMigrator;
using Microsoft.Extensions.Options;
using Yautbox.Postgres.Migrations.Options;
using Yautbox.Postgres.Migrations.Shared;

namespace Yautbox.Postgres.Migrations;

[Migration(1, "InitialMigration")]
internal sealed class InitialMigration : SqlMigration
{
    private readonly MigrationOptions _options;

    public InitialMigration(IOptions<MigrationOptions> options) => _options = options.Value;

    protected override string GetUpSql(IServiceProvider services) =>
        $"""
         CREATE TABLE {_options.SchemaName}.outbox_messages
         (
             id           BIGINT GENERATED ALWAYS AS IDENTITY,
             type         TEXT                     NOT NULL,
             payload      jsonb                    NOT NULL,
             created_at   TIMESTAMP WITH TIME ZONE NOT NULL,
             attempt      INT                      NOT NULL DEFAULT 0,
             scheduled_at TIMESTAMP WITH TIME ZONE NULL,
             is_deleted   BOOL                     NOT NULL,
             locker       TIMESTAMP                NULL,
             PRIMARY KEY (id, is_deleted)
         ) PARTITION BY LIST (is_deleted);

         CREATE TABLE {_options.SchemaName}.outbox_messages_active PARTITION OF {_options.SchemaName}.outbox_messages FOR VALUES IN (false);
         CREATE TABLE {_options.SchemaName}.outbox_messages_deleted PARTITION OF {_options.SchemaName}.outbox_messages FOR VALUES IN (true);
         """;

    protected override string GetDownSql(IServiceProvider services) =>
        $"""
         DROP TABLE {_options.SchemaName}.outbox_messages;
         """;
}
