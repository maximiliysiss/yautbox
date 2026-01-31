using System;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Yautbox.Extensions.Ioc;
using Yautbox.InMemory.Extensions;
using Yautbox.InMemory.Options;
using Yautbox.InMemory.IntegrationTests.Shared.State;
using Yautbox.Runner.Options;

namespace Yautbox.InMemory.IntegrationTests.Shared.Fixture;

public sealed class InMemoryOutboxIntegrationTestFixture : WebApplicationFactory<InMemoryOutboxIntegrationTestFixture.Startup>
{
    protected override IHostBuilder? CreateHostBuilder() => Host.CreateDefaultBuilder();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder
            .UseStartup<Startup>()
            .UseContentRoot(Directory.GetCurrentDirectory());
    }

    public sealed class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddOutbox(b => b.UseInMemory(new InMemoryOutboxOptions { Capacity = 100 }));

            services
                .AddOutboxHandler<IntegrationTestState.MessageA, IntegrationTestState.HandlerA>()
                .ConfigureOptions<DefaultRunnerOptions>(options => options.Configure(ConfigureRunnerOptions));

            services
                .AddOutboxHandler<IntegrationTestState.MessageB, IntegrationTestState.HandlerB>()
                .ConfigureOptions<DefaultRunnerOptions>(options => options.Configure(ConfigureRunnerOptions));

            services
                .AddOutboxHandler<IntegrationTestState.ScheduledMessage, IntegrationTestState.ScheduledHandler>()
                .ConfigureOptions<DefaultRunnerOptions>(options => options.Configure(ConfigureRunnerOptions));

            services
                .AddOutboxHandler<IntegrationTestState.CancelMessage, IntegrationTestState.CancelHandler>()
                .ConfigureOptions<DefaultRunnerOptions>(options => options.Configure(ConfigureRunnerOptions));

            services
                .AddOutboxHandler<IntegrationTestState.RetryMessage, IntegrationTestState.RetryHandler>()
                .ConfigureOptions<DefaultRunnerOptions>(options => options.Configure(ConfigureRunnerOptions));
        }

        public void Configure(IApplicationBuilder app)
        {
        }

        private static void ConfigureRunnerOptions(DefaultRunnerOptions options)
        {
            options.PollDelay = TimeSpan.FromMilliseconds(25);
            options.FailureDelay = TimeSpan.FromMilliseconds(25);
            options.Visibility = TimeSpan.FromMilliseconds(100);
            options.BufferSize = 10;
            options.PerBufferCount = 10;
            options.WorkersCount = 1;
            options.BackupInterval = null;
        }

        private sealed class DefaultRunnerOptions : IOutboxRunnerOptions
        {
            public string? Identifier { get; set; }
            public TimeSpan PollDelay { get; set; }
            public int BufferSize { get; set; }
            public TimeSpan HandleTimeout { get; } = TimeSpan.FromMinutes(30);
            public bool IsEnabled { get; set; }
            public int WorkersCount { get; set; }
            public OutboxDeletePolicy DeletePolicy { get; set; }
            public TimeSpan FailureDelay { get; set; }
            public TimeSpan Visibility { get; set; }
            public TimeSpan? BackupInterval { get; set; }
            public OutboxExecutionPolicy ExecutionPolicy { get; set; }
            public int PerBufferCount { get; set; }
        }
    }
}
