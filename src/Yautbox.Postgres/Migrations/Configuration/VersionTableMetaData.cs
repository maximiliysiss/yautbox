using Yautbox.Postgres.Migrations.Options;

namespace Yautbox.Postgres.Migrations.Configuration;

public sealed class VersionTableMetaData : IVersionTableMetaData
{
    private readonly MigrationOptions _options;

    public VersionTableMetaData(IOptions<RunnerOptions> runnerOptions, IOptions<MigrationOptions> options)
    {
#pragma warning disable CS0618
        ApplicationContext = runnerOptions.Value.ApplicationContext;
#pragma warning restore CS0618
        _options = options.Value;
    }

    public object ApplicationContext { get; set; }

    public bool OwnsSchema => false;

    public string SchemaName => _options.SchemaName;

    public string TableName => "version_info";

    public string ColumnName => "version";

    public string UniqueIndexName => "version_info_version_idx";

    public string AppliedOnColumnName => "applied_on";

    public string DescriptionColumnName => "description";
}
