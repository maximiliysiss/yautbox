using Microsoft.Extensions.DependencyInjection;
using Yautbox.Infrastructure.Waiter;
using Yautbox.Provider;
using Yautbox.Registy;
using Yautbox.Runner.Infrastructure;

namespace Yautbox.Extensions.Builders.Outbox;

/// <summary>
/// Builds and configures outbox infrastructure services.
/// </summary>
public interface IOutboxInfrastructureBuilder
{
    /// <summary>
    /// Gets the service collection used for registrations.
    /// </summary>
    IServiceCollection Services { get; }

    /// <summary>
    /// Configures and registers an outbox provider of the specified type with the specified service lifetime.
    /// </summary>
    /// <typeparam name="T">The type of the outbox provider to register. Must implement <see cref="IOutboxProvider"/>.</typeparam>
    /// <param name="lifetime">The desired service lifetime for the outbox provider. Defaults to <see cref="ServiceLifetime.Scoped"/>.</param>
    /// <returns>The current instance of <see cref="IOutboxInfrastructureBuilder"/>, configured with the specified outbox provider.</returns>
    IOutboxInfrastructureBuilder SetProvider<T>(ServiceLifetime lifetime = ServiceLifetime.Scoped) where T : class, IOutboxProvider;

    /// <summary>
    /// Configures and registers an infrastructure readiness waiter of the specified type with the specified service lifetime.
    /// </summary>
    /// <typeparam name="T">The type of the infrastructure readiness waiter to register. Must implement <see cref="IInfrastructureReadinessWaiter"/>.</typeparam>
    /// <param name="lifetime">The desired service lifetime for the infrastructure readiness waiter. Defaults to <see cref="ServiceLifetime.Singleton"/>.</param>
    /// <returns>The current instance of <see cref="IOutboxInfrastructureBuilder"/>, configured with the specified infrastructure readiness waiter.</returns>
    IOutboxInfrastructureBuilder SetWaiter<T>(ServiceLifetime lifetime = ServiceLifetime.Singleton)
        where T : class, IInfrastructureReadinessWaiter;

    /// <summary>
    /// Configures and registers a policy factory of the specified type with the specified service lifetime.
    /// </summary>
    /// <typeparam name="T">The type of the policy factory to register. Must implement <see cref="IPolicyFactory"/>.</typeparam>
    /// <param name="lifetime">The desired service lifetime for the policy factory. Defaults to <see cref="ServiceLifetime.Scoped"/>.</param>
    /// <returns>The current instance of <see cref="IOutboxInfrastructureBuilder"/>, configured with the specified policy factory.</returns>
    IOutboxInfrastructureBuilder SetPolicy<T>(ServiceLifetime lifetime = ServiceLifetime.Scoped) where T : class, IPolicyFactory;

    /// <summary>
    /// Sets a custom prefix to be used for outbox-related configurations and registry entries.
    /// </summary>
    /// <param name="prefix">The custom string prefix to configure for the outbox settings.</param>
    /// <returns>The current instance of <see cref="IOutboxInfrastructureBuilder"/>, configured with the specified prefix.</returns>
    IOutboxInfrastructureBuilder SetPrefix(string prefix);

    /// <summary>
    /// Configures the registry policy for the Outbox infrastructure.
    /// </summary>
    /// <param name="policy">The desired registry policy, determining the behavior of the Outbox registry. Must be a value of <see cref="OutboxRegistryPolicy"/>.</param>
    /// <returns>The current instance of <see cref="IOutboxInfrastructureBuilder"/>, configured with the specified registry policy.</returns>
    IOutboxInfrastructureBuilder SetRegistryPolicy(OutboxRegistryPolicy policy);
}
