using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Yautbox.Runner.Options;

namespace Yautbox.Extensions.Builders.Handler;

/// <summary>
/// Builds registrations for outbox handlers and their options.
/// </summary>
public interface IOutboxHandlerBuilder
{
    /// <summary>
    /// Gets the service collection used for registrations.
    /// </summary>
    public IServiceCollection Services { get; }

    /// <summary>
    /// Configures options for the specified outbox runner options type.
    /// </summary>
    /// <typeparam name="T">Options type to configure.</typeparam>
    /// <param name="configureOptions">Optional configuration callback.</param>
    /// <returns>The same builder instance for chaining.</returns>
    IOutboxHandlerBuilder ConfigureOptions<T>(Action<OptionsBuilder<T>>? configureOptions = null) where T : class, IOutboxRunnerOptions;
}
