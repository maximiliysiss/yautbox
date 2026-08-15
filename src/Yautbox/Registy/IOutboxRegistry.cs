using System;
using Yautbox.Runner.Options;

namespace Yautbox.Registy;

internal interface IOutboxRegistry
{
    string GetIdentifier<T>() => GetIdentifier(typeof(T));
    string GetIdentifier(Type type);

    DeletePolicy GetCancellationPolicy<T>();
}
