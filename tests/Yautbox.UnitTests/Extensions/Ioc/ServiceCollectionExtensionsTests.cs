using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Xunit;
using Yautbox.Entities;
using Yautbox.Extensions.Ioc;
using Yautbox.Extensions.Types;
using Yautbox.Handlers;
using Yautbox.Infrastructure.DateTime;
using Yautbox.Provider;
using Yautbox.Registy;
using Yautbox.Runner.Options;
using Yautbox.Services;

namespace Yautbox.UnitTests.Extensions.Ioc;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddOutbox_ShouldRegisterCoreServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddOutbox(builder => builder.SetProvider<TestOutboxProvider>());

        // Assert
        services.Should().ContainSingle(descriptor =>
            descriptor.ServiceType == typeof(IDateTimeProvider) &&
            descriptor.ImplementationType == typeof(DateTimeProvider) &&
            descriptor.Lifetime == ServiceLifetime.Singleton);

        services.Should().ContainSingle(descriptor =>
            descriptor.ServiceType == typeof(IOutboxService) &&
            descriptor.ImplementationType == typeof(OutboxService) &&
            descriptor.Lifetime == ServiceLifetime.Scoped);

        services.Should().ContainSingle(descriptor =>
            descriptor.ServiceType == typeof(IOutboxRegistry) &&
            descriptor.ImplementationType == typeof(OutboxRegistry) &&
            descriptor.Lifetime == ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddOutboxHandler_ShouldRegisterHandlerAndHostedServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddOutboxHandler<MessageA, HandlerA>(ServiceLifetime.Transient);

        // Assert
        services.Should().ContainSingle(descriptor =>
            descriptor.ServiceType == typeof(HandlerA) &&
            descriptor.ImplementationType == typeof(HandlerA) &&
            descriptor.Lifetime == ServiceLifetime.Transient);

        services.Count(descriptor => descriptor.ServiceType == typeof(IHostedService))
            .Should().Be(2);
    }

    [Fact]
    public void AddOutboxHandler_ShouldRegisterMultipleIdentifiers_InRegistryOptions()
    {
        // Arrange
        var serviceProvider = CreateProviderWithHandlers();
        var expectedA = typeof(MessageA).GetVersionFreeFullName();
        var expectedB = typeof(MessageB).GetVersionFreeFullName();

        // Act
        var options = serviceProvider.GetRequiredService<IOptions<OutboxRegistryOptions>>().Value;

        // Assert
        options.Identifiers.Should().ContainKey(typeof(MessageA)).WhoseValue.Should().Be(expectedA);
        options.Identifiers.Should().ContainKey(typeof(MessageB)).WhoseValue.Should().Be(expectedB);
    }

    [Fact]
    public void AddOutboxHandler_ShouldRegisterMultipleIdentifiers_InRegistry()
    {
        // Arrange
        var serviceProvider = CreateProviderWithHandlers();
        var expectedA = typeof(MessageA).GetVersionFreeFullName();
        var expectedB = typeof(MessageB).GetVersionFreeFullName();

        // Act
        using var scope = serviceProvider.CreateScope();
        var registry = scope.ServiceProvider.GetRequiredService<IOutboxRegistry>();

        // Assert
        registry.GetIdentifier<MessageA>().Should().Be(expectedA);
        registry.GetIdentifier<MessageB>().Should().Be(expectedB);
    }

    private static ServiceProvider CreateProviderWithHandlers()
    {
        var services = new ServiceCollection();

        services.AddOutbox(builder => builder.SetProvider<TestOutboxProvider>());
        services.AddOutboxHandler<MessageA, HandlerA>();
        services.AddOutboxHandler<MessageB, HandlerB>();

        return services.BuildServiceProvider();
    }

    private sealed class MessageA;

    private sealed class MessageB;

    private sealed class HandlerA : IOutboxHandler<MessageA>
    {
        public Task HandleAsync(IEnumerable<Handlers.OutboxMessage<MessageA>> messages, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    private sealed class HandlerB : IOutboxHandler<MessageB>
    {
        public Task HandleAsync(IEnumerable<Handlers.OutboxMessage<MessageB>> messages, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    private sealed class TestOutboxProvider : IOutboxProvider
    {
        public Task<IReadOnlyCollection<Entities.OutboxMessage<T>>> GetAsync<T>(
            string identifier,
            int count,
            TimeSpan visibility,
            CancellationToken cancellationToken) => throw new NotImplementedException();

        public Task<IReadOnlyCollection<OutboxMessageId>> AddAsync<T>(
            string identifier,
            IReadOnlyCollection<Entities.OutboxMessage<T>> messages,
            CancellationToken cancellationToken) => throw new NotImplementedException();

        public Task CancelAsync(
            string identifier,
            IReadOnlyCollection<OutboxMessageId> ids,
            DeletePolicy policy,
            CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task DeleteAsync(
            string identifier,
            IReadOnlyCollection<OutboxMessageId> ids,
            DeletePolicy policy,
            CancellationToken cancellationToken) => throw new NotImplementedException();

        public Task CleanAsync(string identifier, DateTimeOffset olderThan, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task RetryAsync<T>(
            string identifier,
            IReadOnlyCollection<Entities.OutboxMessage<T>> messages,
            CancellationToken cancellationToken) => throw new NotImplementedException();
    }
}
