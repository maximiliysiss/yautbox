using FluentMigrator.Runner.Initialization;
using FluentMigrator.Runner.VersionTableInfo;

namespace Yautbox.Mysql.Migrations.Infrastructure;

internal sealed class VersionTableMetaDataAccessor(IVersionTableMetaData versionTableMetaData) : IVersionTableMetaDataAccessor
{
    public IVersionTableMetaData VersionTableMetaData { get; } = versionTableMetaData;
}
