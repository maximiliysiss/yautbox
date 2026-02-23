using System;
using FluentMigrator;
using FluentMigrator.Expressions;
using FluentMigrator.Infrastructure;

namespace Yautbox.Postgres.Migrations.Shared;

internal abstract class SqlMigration : IMigration
{
    public void GetUpExpressions(IMigrationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.Expressions.Add(new ExecuteSqlStatementExpression { SqlStatement = GetUpSql(context.ServiceProvider) });
    }

    public void GetDownExpressions(IMigrationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.Expressions.Add(new ExecuteSqlStatementExpression { SqlStatement = GetDownSql(context.ServiceProvider) });
    }

    protected abstract string GetUpSql(IServiceProvider services);
    protected abstract string GetDownSql(IServiceProvider services);

    string IMigration.ConnectionString => throw new NotSupportedException();
}
