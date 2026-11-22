using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;
using Yautbox.Extensions.Ioc;
using Yautbox.Handlers;
using Yautbox.InMemory.Extensions;
using Yautbox.Runner.Options;
using Yautbox.Services;

namespace Yautbox.InMemory.IntegrationTests.Cases;

/// <summary>
/// Covers runtime options reload: a handler bound to <see cref="IConfiguration"/> restarts with the new values
/// when the configuration is reloaded, without restarting the host.
/// </summary>
public sealed class RealtimeConfigChangeTests
{
    private const string IsEnabledKey = "Outbox:RealtimeConfigChange:IsEnabled";

    [Fact]
    public async Task ConfigurationReload_ShouldActivateDisabledHandler()
    {
        // Arrange
        var configuration = (IConfigurationRoot)new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { [IsEnabledKey] = "false" })
            .Build();

        var handler = new Handler();

        using var host = new HostBuilder()
            .ConfigureServices(services =>
            {
                services.AddSingleton<IConfiguration>(configuration);
                services.AddSingleton(handler);
                services.AddOutbox(builder => builder.UseInMemory());
                services
                    .AddOutboxHandler<Message, Handler>()
                    .ConfigureOptions<Options>(options => options.Bind(configuration.GetSection("Outbox:RealtimeConfigChange")));
            })
            .Build();

        await host.StartAsync();

        try
        {
            using (var scope = host.Services.CreateScope())
            {
                await scope.ServiceProvider
                    .GetRequiredService<IOutboxService>()
                    .HandleAsync(messages: [new Message(1)]);
            }

            // Act
            await Task.Delay(TimeSpan.FromSeconds(1));
            var countWhileDisabled = handler.Count;

            configuration[IsEnabledKey] = "true";
            configuration.Reload();

            await WaitUntilAsync(() => handler.Count > 0, TimeSpan.FromSeconds(10));

            // Assert
            countWhileDisabled.Should().Be(0, "a disabled handler must not process messages");
            handler.Count.Should().BeGreaterThan(0, "the reload must restart the handler with IsEnabled = true");
        }
        finally
        {
            await host.StopAsync();
        }
    }

    private static async Task WaitUntilAsync(Func<bool> condition, TimeSpan limit)
    {
        using var timeout = new CancellationTokenSource(limit);

        while (!condition() && !timeout.IsCancellationRequested)
            await Task.Delay(TimeSpan.FromMilliseconds(50), CancellationToken.None);
    }

    public sealed record Message(int Id);

    public sealed class Handler : IOutboxHandler<Message>
    {
        private int _count;

        public int Count => Volatile.Read(ref _count);

        public Task HandleAsync(IEnumerable<OutboxMessage<Message>> messages, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _count);
            return Task.CompletedTask;
        }
    }

    public sealed class Options : IOutboxRunnerOptions<Message>
    {
        public string? Identifier { get; set; } = "realtime-config-change";
        public TimeSpan PollDelay { get; set; } = TimeSpan.FromMilliseconds(50);
        public int BufferSize { get; set; } = 10;
        public TimeSpan HandleTimeout { get; set; } = TimeSpan.FromMinutes(1);
        public bool IsEnabled { get; set; }
        public int WorkersCount { get; set; } = 1;
        public int PerBufferCount { get; set; } = 10;
        public DeletePolicy DeletePolicy { get; set; } = DeletePolicy.Safe;
        public TimeSpan FailureDelay { get; set; } = TimeSpan.FromMilliseconds(50);
        public TimeSpan Visibility { get; set; } = TimeSpan.FromMinutes(1);
        public TimeSpan? BackupInterval { get; set; }
        public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromDays(1);
        public ExecutionPolicy ExecutionPolicy { get; set; } = ExecutionPolicy.Parallel;
        public TimeSpan PolicyTimeout { get; set; } = TimeSpan.FromSeconds(5);
        public DeletePolicy CancellationPolicy { get; set; } = DeletePolicy.Safe;
        public ScopeLifetime ScopeLifetime { get; set; } = ScopeLifetime.PerBatch;
    }
}
