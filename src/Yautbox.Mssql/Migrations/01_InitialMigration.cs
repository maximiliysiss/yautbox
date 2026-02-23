using System;
using FluentMigrator;
using Microsoft.Extensions.Options;
using Yautbox.Mssql.Migrations.Options;
using Yautbox.Mssql.Migrations.Shared;

namespace Yautbox.Mssql.Migrations;

[Migration(1, "InitialMigration")]
internal sealed class InitialMigration : SqlMigration
{
    private readonly MigrationOptions _options;

    public InitialMigration(IOptions<MigrationOptions> options) => _options = options.Value;

    protected override string GetUpSql(IServiceProvider services) =>
        $"""
         IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = '{_options.SchemaName}')
             EXEC('CREATE SCHEMA [{_options.SchemaName}]');

         IF NOT EXISTS (
             SELECT 1
             FROM sys.types t
             INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
             WHERE t.name = 'outbox_message_table_type' AND s.name = '{_options.SchemaName}')
         BEGIN
             EXEC('CREATE TYPE [{_options.SchemaName}].[outbox_message_table_type] AS TABLE (
                 payload NVARCHAR(MAX) NOT NULL,
                 attempt INT NOT NULL,
                 scheduled_at DATETIMEOFFSET NULL,
                 created_at DATETIMEOFFSET NOT NULL
             )');
         END

         IF NOT EXISTS (
             SELECT 1
             FROM sys.types t
             INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
             WHERE t.name = 'outbox_id_table_type' AND s.name = '{_options.SchemaName}')
         BEGIN
             EXEC('CREATE TYPE [{_options.SchemaName}].[outbox_id_table_type] AS TABLE (
                 id BIGINT NOT NULL
             )');
         END

         IF NOT EXISTS (
             SELECT 1
             FROM sys.types t
             INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
             WHERE t.name = 'outbox_update_table_type' AND s.name = '{_options.SchemaName}')
         BEGIN
             EXEC('CREATE TYPE [{_options.SchemaName}].[outbox_update_table_type] AS TABLE (
                 id BIGINT NOT NULL,
                 attempt INT NOT NULL,
                 scheduled_at DATETIMEOFFSET NULL
             )');
         END

         IF NOT EXISTS (
             SELECT 1
             FROM sys.tables t
             INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
             WHERE t.name = 'outbox_messages' AND s.name = '{_options.SchemaName}')
         BEGIN
             CREATE TABLE [{_options.SchemaName}].[outbox_messages]
             (
                 id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                 type NVARCHAR(450) NOT NULL,
                 payload NVARCHAR(MAX) NOT NULL,
                 created_at DATETIMEOFFSET NOT NULL,
                 attempt INT NOT NULL DEFAULT(0),
                 scheduled_at DATETIMEOFFSET NULL,
                 is_deleted BIT NOT NULL DEFAULT(0),
                 locker DATETIMEOFFSET NULL
             );

             CREATE INDEX IX_outbox_messages_active ON [{_options.SchemaName}].[outbox_messages]
                 (type, scheduled_at, locker, id)
                 WHERE is_deleted = 0;

             CREATE INDEX IX_outbox_messages_deleted ON [{_options.SchemaName}].[outbox_messages]
                 (type, created_at, id)
                 WHERE is_deleted = 1;
         END
         """;

    protected override string GetDownSql(IServiceProvider services) =>
        $"""
         IF EXISTS (
             SELECT 1
             FROM sys.tables t
             INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
             WHERE t.name = 'outbox_messages' AND s.name = '{_options.SchemaName}')
         BEGIN
             DROP TABLE [{_options.SchemaName}].[outbox_messages];
         END

         IF EXISTS (
             SELECT 1
             FROM sys.types t
             INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
             WHERE t.name = 'outbox_message_table_type' AND s.name = '{_options.SchemaName}')
         BEGIN
             DROP TYPE [{_options.SchemaName}].[outbox_message_table_type];
         END

         IF EXISTS (
             SELECT 1
             FROM sys.types t
             INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
             WHERE t.name = 'outbox_id_table_type' AND s.name = '{_options.SchemaName}')
         BEGIN
             DROP TYPE [{_options.SchemaName}].[outbox_id_table_type];
         END

         IF EXISTS (
             SELECT 1
             FROM sys.types t
             INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
             WHERE t.name = 'outbox_update_table_type' AND s.name = '{_options.SchemaName}')
         BEGIN
             DROP TYPE [{_options.SchemaName}].[outbox_update_table_type];
         END
         """;
}
