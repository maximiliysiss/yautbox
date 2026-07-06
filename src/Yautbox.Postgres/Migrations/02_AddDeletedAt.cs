using System;
using FluentMigrator;
using Microsoft.Extensions.Options;
using Yautbox.Postgres.Migrations.Options;
using Yautbox.Postgres.Migrations.Shared;

namespace Yautbox.Postgres.Migrations;

[Migration(2, "AddDeletedAt")]
internal sealed class AddDeletedAt : SqlMigration
{
    private readonly MigrationOptions _options;

    public AddDeletedAt(IOptions<MigrationOptions> options) => _options = options.Value;

    protected override string GetUpSql(IServiceProvider services) =>
        $"""
         ALTER TABLE {_options.SchemaName}.outbox_messages
         ADD COLUMN deleted_at TIMESTAMP WITH TIME ZONE NULL;
         """;

    protected override string GetDownSql(IServiceProvider services) =>
        $"""
         ALTER TABLE {_options.SchemaName}.outbox_messages
         DROP COLUMN deleted_at;
         """;
}
