using System.Data.Common;
using NpgsqlTypes;

// ReSharper disable once CheckNamespace
namespace Npgsql;

internal static class NpgsqlParameterCollectionExtensions
{
    public static void Add<T>(this DbParameterCollection parameters, string name, T value) =>
        parameters.Add(
            new NpgsqlParameter<T>
            {
                ParameterName = name,
                TypedValue = value
            });

    public static void Add<T>(this DbParameterCollection parameters, string name, T value, NpgsqlDbType npgsqlDbType) =>
        parameters.Add(
            new NpgsqlParameter<T>(name, npgsqlDbType)
            {
                TypedValue = value
            });
}
