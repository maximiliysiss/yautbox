using System.Text.Json;
using Yautbox.Mssql.Environment;

namespace Yautbox.Mssql.Options;

internal sealed class MssqlOutboxRepositoryOptions
{
    public JsonSerializerOptions JsonSerializerOptions { get; } = new();
    public string SchemaName { get; set; } = MssqlDefaultEnvironment.DefaultSchema;
    public int CleanupBatchSize { get; set; } = 1000;
}
