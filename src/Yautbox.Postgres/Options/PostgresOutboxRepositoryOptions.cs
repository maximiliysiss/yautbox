using System.Text.Json;
using Yautbox.Postgres.Environment;

namespace Yautbox.Postgres.Options;

internal sealed class PostgresOutboxRepositoryOptions
{
    public JsonSerializerOptions JsonSerializerOptions { get; } = new();
    public string SchemaName { get; set; } = PostgresDefaultEnvironment.DefaultSchema;
}
