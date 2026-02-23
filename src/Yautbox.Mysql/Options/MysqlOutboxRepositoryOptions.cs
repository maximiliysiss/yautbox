using System.Text.Json;
using Yautbox.Mysql.Environment;

namespace Yautbox.Mysql.Options;

internal sealed class MysqlOutboxRepositoryOptions
{
    public JsonSerializerOptions JsonSerializerOptions { get; } = new();
    public string SchemaName { get; set; } = MysqlDefaultEnvironment.DefaultSchema;
    public int CleanupBatchSize { get; set; } = 1000;
}
