using System;
using System.Collections.Generic;
using Microsoft.Extensions.Options;
using Yautbox.Exceptions;
using Yautbox.Extensions.Types;
using Yautbox.Runner.Options;

namespace Yautbox.Registy;

internal sealed class OutboxRegistryOptions
{
    public Dictionary<Type, string> Identifiers { get; } = [];
    public Dictionary<Type, DeletePolicy> CancellingPolicies { get; } = [];
    public string? Prefix { get; set; }
    public OutboxRegistryPolicy Policy { get; set; } = OutboxRegistryPolicy.Lenient;

    public void Register<T>(IOptionsMonitor<IOutboxRunnerOptions> monitor)
    {
        var type = typeof(T);

        if (!Identifiers.TryAdd(type, monitor.CurrentValue.Identifier ?? type.GetVersionFreeFullName()))
            throw new HandlerAlreadyAddedException(type);

        if (!CancellingPolicies.TryAdd(type, monitor.CurrentValue.CancellationPolicy))
            throw new HandlerAlreadyAddedException(type);
    }
}
