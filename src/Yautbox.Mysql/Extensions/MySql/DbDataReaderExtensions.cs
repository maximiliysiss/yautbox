using System.Data.Common;

namespace Yautbox.Mysql.Extensions.MySql;

internal static class DbDataReaderExtensions
{
    public static string? GetNullableString(this DbDataReader reader, string name)
    {
        var ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }

    public static System.DateTime? GetNullableDateTime(this DbDataReader reader, string name)
    {
        var ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal) ? null : reader.GetDateTime(ordinal);
    }
}
