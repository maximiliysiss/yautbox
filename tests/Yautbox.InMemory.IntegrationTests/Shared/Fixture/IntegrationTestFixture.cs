using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Yautbox.Extensions.Ioc;
using Yautbox.InMemory.Extensions;
using Yautbox.InMemory.IntegrationTests.Cases;
using Yautbox.InMemory.IntegrationTests.Shared.Options;

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
                .ConfigureOptions<TestRunnerOptions>();

            services
                .AddOutboxHandler<CustomIdentifierOutboxHandlerTests.Message, CustomIdentifierOutboxHandlerTests.Handler>()
                .ConfigureOptions<CustomIdentifierRunnerOptions>();

            services
                .AddOutboxHandler<DisabledOutboxHandlerTests.Message, DisabledOutboxHandlerTests.Handler>()
                .ConfigureOptions<DisabledRunnerOptions>();

            services
                .AddOutboxHandler<CancelledOutboxHandlerTests.Message, CancelledOutboxHandlerTests.Handler>()
                .ConfigureOptions<TestRunnerOptions>();

            services
                .AddOutboxHandler<ScheduledOutboxHandlerTests.Message, ScheduledOutboxHandlerTests.Handler>()
                .ConfigureOptions<TestRunnerOptions>();

            services
                .AddOutboxHandler<RetryOutboxHandlerTests.Message, RetryOutboxHandlerTests.Handler>()
                .ConfigureOptions<TestRunnerOptions>();

            services
                .AddOutboxHandler<ExplicitRetryOutboxHandlerTests.Message, ExplicitRetryOutboxHandlerTests.Handler>()
                .ConfigureOptions<TestRunnerOptions>();

            services
                .AddOutboxHandler<TransactionScopeOutboxHandlerTests.Message, TransactionScopeOutboxHandlerTests.Handler>()
                .ConfigureOptions<TestRunnerOptions>();

            services
                .AddOutboxHandler<VisibilityTimeoutOutboxHandlerTests.Message, VisibilityTimeoutOutboxHandlerTests.Handler>()
                .ConfigureOptions<VisibilityTimeoutRunnerOptions>();

            services
                .AddOutboxHandler<HandleTimeoutOutboxHandlerTests.Message, HandleTimeoutOutboxHandlerTests.Handler>()
                .ConfigureOptions<HandleTimeoutRunnerOptions>();

            services
                .AddOutboxHandler<WorkersPerBufferOutboxHandlerTests.Message, WorkersPerBufferOutboxHandlerTests.Handler>()
                .ConfigureOptions<WorkersRunnerOptions>();

            services
                .AddOutboxHandler<DeletePolicyDeleteOutboxHandlerTests.Message, DeletePolicyDeleteOutboxHandlerTests.Handler>()
                .ConfigureOptions<DeletePolicyDeleteRunnerOptions>();

            services
                .AddOutboxHandler<BackupIntervalOutboxHandlerTests.Message, BackupIntervalOutboxHandlerTests.Handler>()
                .ConfigureOptions<BackupIntervalRunnerOptions>();

            services
                .AddOutboxHandler<SequentialExecutionOutboxHandlerTests.Message, SequentialExecutionOutboxHandlerTests.Handler>()
                .ConfigureOptions<SequentialExecutionRunnerOptions>();

            services
                .AddOutboxHandler<MultipleMessagesOutboxHandlerTests.Message, MultipleMessagesOutboxHandlerTests.Handler>()
                .ConfigureOptions<TestRunnerOptions>();
        }

        public void Configure(IApplicationBuilder app)
        {
        }
    }
}
