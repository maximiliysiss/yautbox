using System;

namespace Yautbox.Extensions.Types;

internal static class TypeExtensions
{
    public static string GetVersionFreeFullName(this Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentException.ThrowIfNullOrEmpty(type.FullName);

        var assemblyName = type.Assembly.GetName();

        ArgumentException.ThrowIfNullOrEmpty(assemblyName.Name);

        return $"{type.FullName}, {assemblyName.Name}";
    }
}
