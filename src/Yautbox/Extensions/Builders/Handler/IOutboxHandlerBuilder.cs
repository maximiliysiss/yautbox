using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Yautbox.Runner.Options;

namespace Yautbox.Extensions.Builders.Handler;

/// <summary>
/// Builds registrations for an outbox handler of payload type <typeparamref name="TPayload"/> and its options.
/// </summary>
/// <typeparam name="TPayload">Payload type handled by the handler.</typeparam>
public interface IOutboxHandlerBuilder<TPayload>
{
    /// <summary>
    /// Gets the service collection used for registrations.
    /// </summary>
    public IServiceCollection Services { get; }

    /// <summary>
    /// Configures options for the specified outbox runner options type.
    /// </summary>
    /// <typeparam name="T">Options type to configure. Must implement <see cref="IOutboxRunnerOptions{TPayload}"/> for the handler payload.</typeparam>
    /// <param name="configureOptions">Optional configuration callback.</param>
    /// <returns>The same builder instance for chaining.</returns>
    IOutboxHandlerBuilder<TPayload> ConfigureOptions<T>(Action<OptionsBuilder<T>>? configureOptions = null) where T : class, IOutboxRunnerOptions<TPayload>;
}
