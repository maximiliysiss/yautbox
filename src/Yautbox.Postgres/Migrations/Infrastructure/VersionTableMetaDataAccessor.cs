using FluentMigrator.Runner.Initialization;
using FluentMigrator.Runner.VersionTableInfo;

namespace Yautbox.Postgres.Migrations.Infrastructure;

internal sealed class VersionTableMetaDataAccessor(IVersionTableMetaData versionTableMetaData) : IVersionTableMetaDataAccessor
{
    public IVersionTableMetaData VersionTableMetaData { get; } = versionTableMetaData;
}
