using System;
using System.Collections.Generic;
using Microsoft.Extensions.Options;
using Yautbox.Exceptions;
using Yautbox.Extensions.Types;
using Yautbox.Runner.Options;

namespace Yautbox.Registy;

internal sealed class OutboxRegistry(IOptionsSnapshot<OutboxRegistryOptions> options) : IOutboxRegistry
{
    private readonly OutboxRegistryOptions _options = options.Value;

    public string GetIdentifier(Type type)
    {
        var identifier = _options.Identifiers.GetValueOrDefault(type);

        if (identifier is null && _options.Policy is OutboxRegistryPolicy.Strict)
            throw new RegistryStrictException(type);

        identifier ??= type.GetVersionFreeFullName();

        return $"{_options.Prefix}{identifier}";
    }

    public DeletePolicy GetCancellationPolicy<T>()
    {
        var type = typeof(T);

        var isExists = _options.CancellingPolicies.TryGetValue(type, out var policy);

        if (!isExists && _options.Policy is OutboxRegistryPolicy.Strict)
            throw new RegistryStrictException(type);

        return isExists ? policy : DeletePolicy.Safe;
    }
}
