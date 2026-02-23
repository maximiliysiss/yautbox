using System;
using System.Collections.Generic;
using System.Linq;
using FluentMigrator;
using FluentMigrator.Runner.Initialization;

namespace Yautbox.Postgres.Migrations.Infrastructure;

internal sealed class MigrationSourceItem : IMigrationSourceItem
{
    public MigrationSourceItem()
    {
        MigrationTypeCandidates = typeof(InitialMigration).Assembly
            .GetTypes()
            .Where(t => typeof(IMigration).IsAssignableFrom(t) && !t.IsAbstract)
            .ToArray();
    }

    public IEnumerable<Type> MigrationTypeCandidates { get; }
}
