using System;
using Yautbox.Extensions.Builders;
using Yautbox.Handlers;
using Yautbox.Options;
using Yautbox.Runner;
using Yautbox.Services;

namespace Yautbox.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOutbox(
        this IServiceCollection services,
        Action<IOutboxInfrastructureBuilder> configureInfrastructure,
        Action<OutboxSerializerOptions, IServiceProvider>? configureSerialization = null)
    {
        var builder = new OutboxInfrastructureBuilder(services);
        configureInfrastructure.Invoke(builder);
        builder.Build();

        configureSerialization ??= (_, _) => { };

        services
            .AddOptions<OutboxSerializerOptions>()
            .Configure(configureSerialization)
            .Services
            .AddOptions<DefaultRunnerOptions>();

        services.AddScoped<IOutboxService, OutboxService>();

        return services;
    }

    public static IOutboxHandlerBuilder AddOutboxHandler<TPayload, THandler>(this IServiceCollection services)
        where THandler : class, IOutboxHandler<TPayload>
    {
        var builder = new OutboxHandlerBuilder(services);
        services
            .AddScoped<THandler>()
            .AddSingleton<IHostedService>(
                sp => (IHostedService)ActivatorUtilities.CreateInstance(
                    sp,
                    typeof(OutboxRunner<,,>).MakeGenericType(typeof(THandler), typeof(TPayload), builder._optionsType)));

        return builder;
    }
}
