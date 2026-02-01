using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Yautbox.Runner.Options;

namespace Yautbox.Extensions.Builders.Handler;

internal class OutboxHandlerBuilder(IServiceCollection services) : IOutboxHandlerBuilder
{
    internal Type _optionsType = typeof(DefaultRunnerOptions);

    public IServiceCollection Services { get; } = services;

    public IOutboxHandlerBuilder ConfigureOptions<T>(Action<OptionsBuilder<T>>? configureOptions = null) where T : class, IOutboxRunnerOptions
    {
        _optionsType = typeof(T);
        var optionsBuilder = Services.AddOptions<T>();
        configureOptions?.Invoke(optionsBuilder);

        return this;
    }
}
