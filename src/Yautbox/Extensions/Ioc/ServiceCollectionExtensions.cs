using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Yautbox.Extensions.Builders.Handler;
using Yautbox.Extensions.Builders.Outbox;
using Yautbox.Handlers;
using Yautbox.Infrastructure.DateTime;
using Yautbox.Infrastructure.Waiter;
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

        services.TryAddScoped<IOutboxService, OutboxService>();

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
            .AddHostedService(ConfigureRunner);

        return builder;

        OutboxRunner<THandler, TPayload> ConfigureRunner(IServiceProvider serviceProvider)
        {
            var options = serviceProvider.GetService(builder._optionsType);

            if (options is not null)
                options = new DefaultMonitorOptions((IOutboxRunnerOptions)options);

            options ??= serviceProvider.GetRequiredService(typeof(IOptionsMonitor<>).MakeGenericType(builder._optionsType));

            return ActivatorUtilities.CreateInstance<OutboxRunner<THandler, TPayload>>(serviceProvider, options);
        }
    }

    private sealed class DefaultMonitorOptions(IOutboxRunnerOptions options) : IOptionsMonitor<IOutboxRunnerOptions>
    {
        public IOutboxRunnerOptions Get(string? name) => CurrentValue;

        public IDisposable? OnChange(Action<IOutboxRunnerOptions, string?> listener) => null;

        public IOutboxRunnerOptions CurrentValue { get; } = options;
    }
}
