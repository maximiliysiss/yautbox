using System;
using Yautbox.Runner;

namespace Yautbox.Extensions.Builders;

public interface IOutboxHandlerBuilder
{
    public IServiceCollection Services { get; }

    IOutboxHandlerBuilder ConfigureOptions<TOptions>(Action<OptionsBuilder<TOptions>> configureOptions)
        where TOptions : class, IOutboxRunnerOptions;
}
