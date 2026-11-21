using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Yautbox.Runner.Options;

namespace Yautbox.Extensions.Builders.Handler;

internal class OutboxHandlerBuilder<TPayload>(IServiceCollection services) : IOutboxHandlerBuilder<TPayload>
{
    internal Type _optionsType = typeof(DefaultRunnerOptions<TPayload>);

    public IServiceCollection Services { get; } = services;

    public IOutboxHandlerBuilder<TPayload> ConfigureOptions<T>(Action<OptionsBuilder<T>>? configureOptions = null) where T : class, IOutboxRunnerOptions<TPayload>
    {
        _optionsType = typeof(T);
        var optionsBuilder = Services.AddOptions<T>();
        configureOptions?.Invoke(optionsBuilder);

        return this;
    }
}
