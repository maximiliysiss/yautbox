using System.Collections.Generic;
using Microsoft.Extensions.Options;
using Yautbox.Extensions.Types;
using Yautbox.Runner.Options;

namespace Yautbox.Registy;

internal sealed class OutboxRegistry(IOptionsSnapshot<OutboxRegistryOptions> options) : IOutboxRegistry
{
    private readonly OutboxRegistryOptions _options = options.Value;

    public string GetIdentifier<T>() => _options.Identifiers.GetValueOrDefault(typeof(T)) ?? typeof(T).GetVersionFreeFullName();
    public DeletePolicy GetCancellationPolicy<T>() => _options.CancellingPolicies.GetValueOrDefault(typeof(T), DeletePolicy.Safe);
}
