using System.Data.Common;

namespace Yautbox.Mssql.Extensions.SqlClient;

internal static class DbDataReaderExtensions
{
    public static string? GetNullableString(this DbDataReader reader, string name)
    {
        var ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }
}
