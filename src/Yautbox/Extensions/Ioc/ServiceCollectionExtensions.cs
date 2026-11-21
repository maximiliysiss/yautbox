using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Yautbox.Extensions.Builders.Handler;
using Yautbox.Extensions.Builders.Outbox;
using Yautbox.Handlers;
using Yautbox.Infrastructure.DateTime;
using Yautbox.Metrics;
using Yautbox.Registy;
using Yautbox.Runner;
using Yautbox.Runner.Infrastructure;
using Yautbox.Runner.Options;
using Yautbox.Runner.Validation;
using Yautbox.Services;
using Yautbox.Tracing;

namespace Yautbox.Extensions.Ioc;

/// <summary>
/// Dependency injection helpers for configuring the outbox.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers core outbox services and infrastructure.
    /// </summary>
    /// <param name="services">Service collection to configure.</param>
    /// <param name="configureInfrastructure">Callback to configure infrastructure options.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddOutbox(
        this IServiceCollection services,
        Action<IOutboxInfrastructureBuilder> configureInfrastructure)
    {
        services
            .TryAddSingleton<IDateTimeProvider, DateTimeProvider>();

        var builder = new OutboxInfrastructureBuilder(services);
        configureInfrastructure.Invoke(builder);

        services
            .AddOptions();

        services
            .TryAddScoped<IOutboxService, OutboxService>();

        services
            .TryAddScoped<IPolymorphicOutboxService, OutboxService>();

        services
            .TryAddSingleton<IMetricsHandler, DefaultMetricsHandler>();

        services
            .TryAddSingleton<IOutboxTracer, DefaultOutboxTracer>();

        services
            .TryAddSingleton<IPolicyFactory, DefaultPolicyFactory>();

        services
            .AddOptions<OutboxRegistryOptions>()
            .Services
            .TryAddScoped<IOutboxRegistry, OutboxRegistry>();

        return services;
    }

    /// <summary>
    /// Registers an outbox handler and its background processing services.
    /// </summary>
    /// <typeparam name="TPayload">Payload type handled by the handler.</typeparam>
    /// <typeparam name="THandler">Handler type to register.</typeparam>
    /// <param name="services">Service collection to configure.</param>
    /// <param name="lifetime">Service lifetime for the handler.</param>
    /// <returns>An outbox handler builder for further configuration.</returns>
    public static IOutboxHandlerBuilder<TPayload> AddOutboxHandler<TPayload, THandler>(
        this IServiceCollection services,
        ServiceLifetime lifetime = ServiceLifetime.Scoped)
        where THandler : class, IOutboxHandler<TPayload>
    {
        var builder = new OutboxHandlerBuilder<TPayload>(services);

        services
            .TryAdd(ServiceDescriptor.Describe(typeof(THandler), typeof(THandler), lifetime));

        services
            .TryAddEnumerable(ServiceDescriptor.Singleton<IOutboxOptionsValidator<TPayload>, OutboxOptionsValidator<TPayload>>());

        services
            .AddHostedService(ConfigureRunner)
            .AddHostedService(ConfigureCleaner);

        services
            .AddOptions<OutboxRegistryOptions>()
            .Configure<IServiceProvider>(ConfigureRegistry);

        return builder;

        OutboxHandlerRunner<THandler, TPayload> ConfigureRunner(IServiceProvider serviceProvider)
            => ActivatorUtilities.CreateInstance<OutboxHandlerRunner<THandler, TPayload>>(serviceProvider, CreateOptions(serviceProvider));

        OutboxCleanerRunner<THandler, TPayload> ConfigureCleaner(IServiceProvider serviceProvider)
            => ActivatorUtilities.CreateInstance<OutboxCleanerRunner<THandler, TPayload>>(serviceProvider, CreateOptions(serviceProvider));

        void ConfigureRegistry(OutboxRegistryOptions options, IServiceProvider serviceProvider)
            => options.Register<TPayload>(CreateOptions(serviceProvider));

        IOptionsMonitor<IOutboxRunnerOptions<TPayload>> CreateOptions(IServiceProvider serviceProvider)
        {
            var options = serviceProvider.GetService(builder._optionsType);

            if (options is not null)
                options = new DefaultMonitorOptions<TPayload>((IOutboxRunnerOptions<TPayload>)options);

            options ??= serviceProvider.GetRequiredService(typeof(IOptionsMonitor<>).MakeGenericType(builder._optionsType));

            return (IOptionsMonitor<IOutboxRunnerOptions<TPayload>>)options;
        }
    }

    private sealed class DefaultMonitorOptions<TPayload>(IOutboxRunnerOptions<TPayload> options) : IOptionsMonitor<IOutboxRunnerOptions<TPayload>>
    {
        public IOutboxRunnerOptions<TPayload> Get(string? name) => CurrentValue;

        public IDisposable? OnChange(Action<IOutboxRunnerOptions<TPayload>, string?> listener) => null;

        public IOutboxRunnerOptions<TPayload> CurrentValue { get; } = options;
    }
}
