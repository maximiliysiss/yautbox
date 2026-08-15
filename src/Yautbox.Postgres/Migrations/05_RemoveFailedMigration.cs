using System;
using FluentMigrator;
using FluentMigrator.Expressions;
using FluentMigrator.Infrastructure;
using Microsoft.Extensions.Options;
using Yautbox.Postgres.Migrations.Options;

namespace Yautbox.Postgres.Migrations;

[Migration(5, TransactionBehavior.None, "RemoveFailedMigration")]
internal sealed class RemoveFailedMigration : IMigration
{
    private readonly MigrationOptions _options;

    public RemoveFailedMigration(IOptions<MigrationOptions> options) => _options = options.Value;

    public void GetUpExpressions(IMigrationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.Expressions.Add(new ExecuteSqlStatementExpression
        {
            SqlStatement = $"""
                           DROP INDEX CONCURRENTLY IF EXISTS
                               {_options.SchemaName}.idx__outbox_messages_active__type_id_scheduled_at;
                           """
        });

        context.Expressions.Add(new ExecuteSqlStatementExpression
        {
            SqlStatement = $"""
                           ALTER TABLE {_options.SchemaName}.outbox_messages
                               ALTER COLUMN scheduled_at DROP DEFAULT,
                               ALTER COLUMN scheduled_at DROP NOT NULL;
                           """
        });
    }

    public void GetDownExpressions(IMigrationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.Expressions.Add(new ExecuteSqlStatementExpression { SqlStatement = "SELECT 1;" });
    }

    string IMigration.ConnectionString => throw new NotSupportedException();
}
