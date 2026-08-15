using System;
using FluentMigrator;
using FluentMigrator.Expressions;
using FluentMigrator.Infrastructure;
using Microsoft.Extensions.Options;
using Yautbox.Postgres.Migrations.Options;

namespace Yautbox.Postgres.Migrations;

[Migration(6, TransactionBehavior.None, "AddIndex")]
internal sealed class AddIndex : IMigration
{
    private const string CreateIndexSql = """
        CREATE INDEX CONCURRENTLY IF NOT EXISTS idx__outbox_messages_active__type_id_scheduled_at_coalesce
            ON {0}.outbox_messages_active (type, id, COALESCE(scheduled_at, '-infinity'::timestamptz));
        """;

    private const string DropIndexSql = """
        DROP INDEX CONCURRENTLY IF EXISTS {0}.idx__outbox_messages_active__type_id_scheduled_at_coalesce;
        """;

    private readonly MigrationOptions _options;

    public AddIndex(IOptions<MigrationOptions> options) => _options = options.Value;

    public void GetUpExpressions(IMigrationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.Expressions.Add(new ExecuteSqlStatementExpression
        {
            SqlStatement = string.Format(DropIndexSql, _options.SchemaName)
        });
        context.Expressions.Add(new ExecuteSqlStatementExpression
        {
            SqlStatement = string.Format(CreateIndexSql, _options.SchemaName)
        });
    }

    public void GetDownExpressions(IMigrationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.Expressions.Add(new ExecuteSqlStatementExpression
        {
            SqlStatement = string.Format(DropIndexSql, _options.SchemaName)
        });
    }

    string IMigration.ConnectionString => throw new NotSupportedException();
}
