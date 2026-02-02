using FluentMigrator.Runner.VersionTableInfo;
using Microsoft.Extensions.Options;
using Yautbox.Postgres.Migrations.Options;

namespace Yautbox.Postgres.Migrations.Configuration;

public sealed class VersionTableMetaData(IOptions<MigrationOptions> options) : IVersionTableMetaData
{
    private readonly MigrationOptions _options = options.Value;

    public bool OwnsSchema => false;
    public string SchemaName => _options.SchemaName;
    public string TableName => "version_info";
    public string ColumnName => "version";
    public string UniqueIndexName => "version_info_version_idx";
    public string AppliedOnColumnName => "applied_on";
    public bool CreateWithPrimaryKey => true;
    public string DescriptionColumnName => "description";
}
