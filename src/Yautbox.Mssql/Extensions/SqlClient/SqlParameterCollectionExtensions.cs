using System;
using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;

namespace Yautbox.Mssql.Extensions.SqlClient;

internal static class SqlParameterCollectionExtensions
{
    public static void Add<T>(this DbParameterCollection parameters, string name, T value)
        => parameters.Add(
            new SqlParameter
            {
                ParameterName = name,
                Value = value ?? (object)DBNull.Value
            });

    public static void AddTable(this DbParameterCollection parameters, string name, DataTable table, string typeName)
        => parameters.Add(
            new SqlParameter(name, SqlDbType.Structured)
            {
                TypeName = typeName,
                Value = table
            });
}
