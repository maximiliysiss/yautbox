using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Yautbox.Runner.Options;

namespace Yautbox.Extensions.Builders.Handler;

public interface IOutboxHandlerBuilder
{
    public IServiceCollection Services { get; }
    IOutboxHandlerBuilder ConfigureOptions<T>(Action<OptionsBuilder<T>> configureOptions) where T : class, IOutboxRunnerOptions;
}
