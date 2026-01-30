using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Yautbox.Extensions.Builders.Handler;
using Yautbox.Extensions.Builders.Outbox;
using Yautbox.Handlers;
using Yautbox.Infrastructure.DateTime;
using Yautbox.Infrastructure.Waiter;
using Yautbox.Registy;
using Yautbox.Runner;
using Yautbox.Runner.Options;
using Yautbox.Services;

namespace Yautbox.Extensions.Ioc;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOutbox(
        this IServiceCollection services,
        Action<IOutboxInfrastructureBuilder> configureInfrastructure)
    {
        services
            .TryAddSingleton<IInfrastructureReadinessWaiter, DefaultReadinessWaiter>();

        services
            .TryAddSingleton<IDateTimeProvider, DateTimeProvider>();

        var builder = new OutboxInfrastructureBuilder(services);
        configureInfrastructure.Invoke(builder);

        services
            .AddOptions<DefaultRunnerOptions>();

        services
            .TryAddScoped<IOutboxService, OutboxService>();

        services
            .AddOptions<OutboxRegistryOptions>()
            .Services
            .TryAddScoped<IOutboxRegistry, OutboxRegistry>();

        return services;
    }

    public static IOutboxHandlerBuilder AddOutboxHandler<TPayload, THandler>(
        this IServiceCollection services,
        ServiceLifetime lifetime = ServiceLifetime.Scoped)
        where THandler : class, IOutboxHandler<TPayload>
    {
        var builder = new OutboxHandlerBuilder(services);

        services
            .TryAdd(ServiceDescriptor.Describe(typeof(THandler), typeof(THandler), lifetime));

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

        IOptionsMonitor<IOutboxRunnerOptions> CreateOptions(IServiceProvider serviceProvider)
        {
            var options = serviceProvider.GetService(builder._optionsType);

            if (options is not null)
                options = new DefaultMonitorOptions((IOutboxRunnerOptions)options);

            options ??= serviceProvider.GetRequiredService(typeof(IOptionsMonitor<>).MakeGenericType(builder._optionsType));

            return (IOptionsMonitor<IOutboxRunnerOptions>)options;
        }
    }

    private sealed class DefaultMonitorOptions(IOutboxRunnerOptions options) : IOptionsMonitor<IOutboxRunnerOptions>
    {
        public IOutboxRunnerOptions Get(string? name) => CurrentValue;

        public IDisposable? OnChange(Action<IOutboxRunnerOptions, string?> listener) => null;

        public IOutboxRunnerOptions CurrentValue { get; } = options;
    }
}
