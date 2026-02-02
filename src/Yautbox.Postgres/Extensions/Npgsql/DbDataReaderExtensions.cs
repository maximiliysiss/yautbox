using System.Data;
using System.Data.Common;

namespace Yautbox.Postgres.Extensions.Npgsql;

internal static class DbDataReaderExtensions
{
    public static string? GetNullableString(this DbDataReader reader, string name)
        => reader.IsDBNull(reader.GetOrdinal(name)) ? null : reader.GetString(name);
}
