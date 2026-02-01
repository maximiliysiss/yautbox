using System;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Yautbox.Extensions.Ioc;
using Yautbox.InMemory.Extensions;
using Yautbox.InMemory.IntegrationTests.Cases;
using Yautbox.InMemory.IntegrationTests.Shared.Options;
using Yautbox.Runner.Options;

namespace Yautbox.InMemory.IntegrationTests.Shared.Fixture;

public sealed class IntegrationTestFixture : WebApplicationFactory<IntegrationTestFixture.Startup>
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
                .AddOutbox(b => b.UseInMemory());

            services
                .AddOutboxHandler<SimpleOutboxHandlerTests.Message, SimpleOutboxHandlerTests.Handler>()
                .ConfigureOptions<TestRunnerOptions>(ConfigureTestRunnerOptions);

            services
                .AddOutboxHandler<CustomIdentifierOutboxHandlerTests.Message, CustomIdentifierOutboxHandlerTests.Handler>()
                .ConfigureOptions<CustomIdentifierRunnerOptions>(ConfigureCustomIdentifierOptions);

            services
                .AddOutboxHandler<DisabledOutboxHandlerTests.Message, DisabledOutboxHandlerTests.Handler>()
                .ConfigureOptions<DisabledRunnerOptions>(ConfigureDisabledOptions);

            services
                .AddOutboxHandler<CancelledOutboxHandlerTests.Message, CancelledOutboxHandlerTests.Handler>()
                .ConfigureOptions<TestRunnerOptions>(ConfigureTestRunnerOptions);

            services
                .AddOutboxHandler<ScheduledOutboxHandlerTests.Message, ScheduledOutboxHandlerTests.Handler>()
                .ConfigureOptions<TestRunnerOptions>(ConfigureTestRunnerOptions);

            services
                .AddOutboxHandler<RetryOutboxHandlerTests.Message, RetryOutboxHandlerTests.Handler>()
                .ConfigureOptions<TestRunnerOptions>(ConfigureTestRunnerOptions);

            services
                .AddOutboxHandler<ExplicitRetryOutboxHandlerTests.Message, ExplicitRetryOutboxHandlerTests.Handler>()
                .ConfigureOptions<TestRunnerOptions>(ConfigureTestRunnerOptions);

            services
                .AddOutboxHandler<WorkersPerBufferOutboxHandlerTests.Message, WorkersPerBufferOutboxHandlerTests.Handler>()
                .ConfigureOptions<WorkersRunnerOptions>(ConfigureWorkersOptions);

            services
                .AddOutboxHandler<DeletePolicyDeleteOutboxHandlerTests.Message, DeletePolicyDeleteOutboxHandlerTests.Handler>()
                .ConfigureOptions<DeletePolicyDeleteRunnerOptions>(ConfigureDeletePolicyDeleteOptions);

            services
                .AddOutboxHandler<BackupIntervalOutboxHandlerTests.Message, BackupIntervalOutboxHandlerTests.Handler>()
                .ConfigureOptions<BackupIntervalRunnerOptions>(ConfigureBackupIntervalOptions);

            services
                .AddOutboxHandler<SequentialExecutionOutboxHandlerTests.Message, SequentialExecutionOutboxHandlerTests.Handler>()
                .ConfigureOptions<SequentialExecutionRunnerOptions>(ConfigureSequentialExecutionOptions);

            services
                .AddOutboxHandler<MultipleMessagesOutboxHandlerTests.Message, MultipleMessagesOutboxHandlerTests.Handler>()
                .ConfigureOptions<TestRunnerOptions>(ConfigureTestRunnerOptions);
        }

        public void Configure(IApplicationBuilder app)
        {
        }

        private static void ConfigureTestRunnerOptions(OptionsBuilder<TestRunnerOptions> options)
        {
            options.Configure(o =>
            {
                o.PollDelay = TimeSpan.FromMilliseconds(50);
                o.FailureDelay = TimeSpan.FromMilliseconds(100);
                o.Visibility = TimeSpan.FromMilliseconds(200);
                o.HandleTimeout = TimeSpan.FromSeconds(5);
                o.BufferSize = 64;
                o.PerBufferCount = 32;
                o.WorkersCount = 1;
            });
        }

        private static void ConfigureCustomIdentifierOptions(OptionsBuilder<CustomIdentifierRunnerOptions> options)
        {
            options.Configure(o =>
            {
                o.Identifier = CustomIdentifierRunnerOptions.IdentifierValue;
                o.PollDelay = TimeSpan.FromMilliseconds(50);
                o.FailureDelay = TimeSpan.FromMilliseconds(100);
                o.Visibility = TimeSpan.FromMilliseconds(200);
                o.HandleTimeout = TimeSpan.FromSeconds(5);
                o.BufferSize = 64;
                o.PerBufferCount = 32;
                o.WorkersCount = 1;
            });
        }

        private static void ConfigureDisabledOptions(OptionsBuilder<DisabledRunnerOptions> options)
        {
            options.Configure(o =>
            {
                o.IsEnabled = false;
                o.PollDelay = TimeSpan.FromMilliseconds(50);
                o.FailureDelay = TimeSpan.FromMilliseconds(100);
                o.Visibility = TimeSpan.FromMilliseconds(200);
                o.HandleTimeout = TimeSpan.FromSeconds(5);
                o.BufferSize = 64;
                o.PerBufferCount = 32;
                o.WorkersCount = 1;
            });
        }

        private static void ConfigureWorkersOptions(OptionsBuilder<WorkersRunnerOptions> options)
        {
            options.Configure(o =>
            {
                o.PollDelay = TimeSpan.FromMilliseconds(50);
                o.FailureDelay = TimeSpan.FromMilliseconds(100);
                o.Visibility = TimeSpan.FromMilliseconds(200);
                o.HandleTimeout = TimeSpan.FromSeconds(5);
                o.BufferSize = 6;
                o.PerBufferCount = 2;
                o.WorkersCount = 3;
            });
        }

        private static void ConfigureDeletePolicyDeleteOptions(OptionsBuilder<DeletePolicyDeleteRunnerOptions> options)
        {
            options.Configure(o =>
            {
                o.PollDelay = TimeSpan.FromMilliseconds(50);
                o.FailureDelay = TimeSpan.FromMilliseconds(100);
                o.Visibility = TimeSpan.FromMilliseconds(200);
                o.HandleTimeout = TimeSpan.FromSeconds(5);
                o.BufferSize = 64;
                o.PerBufferCount = 32;
                o.WorkersCount = 1;
                o.DeletePolicy = OutboxDeletePolicy.Delete;
            });
        }

        private static void ConfigureBackupIntervalOptions(OptionsBuilder<BackupIntervalRunnerOptions> options)
        {
            options.Configure(o =>
            {
                o.PollDelay = TimeSpan.FromMilliseconds(50);
                o.FailureDelay = TimeSpan.FromMilliseconds(100);
                o.Visibility = TimeSpan.FromMilliseconds(200);
                o.HandleTimeout = TimeSpan.FromSeconds(5);
                o.BufferSize = 64;
                o.PerBufferCount = 32;
                o.WorkersCount = 1;
                o.BackupInterval = TimeSpan.FromMilliseconds(200);
            });
        }

        private static void ConfigureSequentialExecutionOptions(OptionsBuilder<SequentialExecutionRunnerOptions> options)
        {
            options.Configure(o =>
            {
                o.PollDelay = TimeSpan.FromMilliseconds(50);
                o.FailureDelay = TimeSpan.FromMilliseconds(100);
                o.Visibility = TimeSpan.FromMilliseconds(200);
                o.HandleTimeout = TimeSpan.FromSeconds(5);
                o.BufferSize = 64;
                o.PerBufferCount = 64;
                o.WorkersCount = 1;
                o.ExecutionPolicy = OutboxExecutionPolicy.Sequential;
            });
        }
    }
}
