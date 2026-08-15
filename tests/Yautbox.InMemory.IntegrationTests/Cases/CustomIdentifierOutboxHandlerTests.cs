using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Yautbox.Handlers;
using Yautbox.InMemory.IntegrationTests.Shared.Extensions;
using Yautbox.InMemory.IntegrationTests.Shared.Fixture;
using Yautbox.InMemory.IntegrationTests.Shared.Options;
using Yautbox.Services;
using Microsoft.Extensions.Logging;
namespace Yautbox.InMemory.IntegrationTests.Cases;

[Collection(nameof(IntegrationTestCollection))]
public class CustomIdentifierOutboxHandlerTests(IntegrationTestFixture fixture)
{
    [Fact]
    public async Task Handler_ShouldUseCustomIdentifier()
    {
        // Arrange
        Handler.Reset();
        using var scope = fixture.Services.CreateScope();

        var service = scope.ServiceProvider.GetRequiredService<IOutboxService>();
        var registryType = Type.GetType("Yautbox.Registy.IOutboxRegistry, Yautbox");
        registryType.Should().NotBeNull();
        var registry = scope.ServiceProvider.GetRequiredService(registryType!);

        // Act
        var identifierMethod = registry.GetType().GetMethod("GetIdentifier");
        identifierMethod.Should().NotBeNull();
        var identifier = (string)identifierMethod!
            .Invoke(registry, [typeof(Message)])!;
        await service.HandleAsync(message: new Message(13));

        var handled = await WaitForHandledAsync(TimeSpan.FromSeconds(2));

        // Assert
        identifier.Should().Be(CustomIdentifierRunnerOptions.IdentifierValue);
        handled.Should().BeTrue();
    }

    private static async Task<bool> WaitForHandledAsync(TimeSpan timeout)
    {
        var stopwatch = Stopwatch.StartNew();
        while (stopwatch.Elapsed < timeout)
        {
            if (Handler.CallCount > 0)
                return true;

            await Task.Delay(TimeSpan.FromMilliseconds(20));
        }

        return false;
    }

    public sealed record Message(int Value);

    public sealed class Handler : IOutboxHandler<Message>
    {
        private readonly ILogger<Handler> _logger;
        public Handler(ILogger<Handler> logger)
        {
            _logger = logger;
        }

        private static int _callCount;
        public static int CallCount => Volatile.Read(ref _callCount);

        public static void Reset()
        {
            Interlocked.Exchange(ref _callCount, 0);
        }

        public Task HandleAsync(IEnumerable<OutboxMessage<Message>> messages, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Handling messages.");
            foreach (var _ in messages)
                Interlocked.Increment(ref _callCount);

            return Task.CompletedTask;
        }
    }
}
