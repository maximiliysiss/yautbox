using Yautbox.Runner.Options;

namespace Yautbox.Registy;

internal interface IOutboxRegistry
{
    string GetIdentifier<T>();
    DeletePolicy GetCancellationPolicy<T>();
}
