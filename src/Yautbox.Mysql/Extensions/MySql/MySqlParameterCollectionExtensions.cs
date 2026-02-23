using System;
using System.Data.Common;
using MySqlConnector;

namespace Yautbox.Mysql.Extensions.MySql;

internal static class MySqlParameterCollectionExtensions
{
    public static DbParameter Add<T>(this DbParameterCollection parameters, string name, T value)
    {
        var parameter = new MySqlParameter
        {
            ParameterName = name,
            Value = value ?? (object)DBNull.Value
        };

        parameters.Add(parameter);
        return parameter;
    }
}
