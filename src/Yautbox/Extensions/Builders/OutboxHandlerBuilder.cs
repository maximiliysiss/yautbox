using System;
using Yautbox.Runner;

namespace Yautbox.Extensions.Builders;

internal class OutboxHandlerBuilder : IOutboxHandlerBuilder
{
    internal Type _optionsType = typeof(DefaultRunnerOptions);

    public IServiceCollection Services { get; }

    public OutboxHandlerBuilder(IServiceCollection services)
    {
        Services = services;
    }

    public IOutboxHandlerBuilder ConfigureOptions<TOptions>(Action<OptionsBuilder<TOptions>> configureOptions)
        where TOptions : class, IOutboxRunnerOptions
    {
        _optionsType = typeof(TOptions);
        var optionsBuilder = Services.AddOptions<TOptions>();
        configureOptions.Invoke(optionsBuilder);

        return this;
    }
}
