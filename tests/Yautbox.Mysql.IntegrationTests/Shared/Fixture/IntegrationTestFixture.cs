using System;
using System.Data.Common;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using MySqlConnector;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Yautbox.Extensions.Ioc;
using Yautbox.Mysql.Extensions;
using Yautbox.Mysql.Infrastructure.Database;
using Yautbox.Mysql.IntegrationTests.Cases;
using Yautbox.Mysql.IntegrationTests.DbHelper;
using Yautbox.Mysql.IntegrationTests.DbHelper.Repositories;
using Yautbox.Mysql.IntegrationTests.Shared.Options;
using Yautbox.Mysql.Repositories;
using Yautbox.Registy;

namespace Yautbox.Mysql.IntegrationTests.Shared.Fixture;

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
            var version = $"{RuntimeInformation.FrameworkDescription}_";

            services
                .AddSingleton<OutboxDbHelper>();

            services
                .AddOutbox(b => b
                    .UseMysql<OutboxConnectionFactory>()
                    .SetRegistryPolicy(OutboxRegistryPolicy.Strict)
                    .SetPrefix(version));

            services
                .Decorate<IMysqlOutboxRepository, TrackingOutboxRepository>();

            services
                .AddOutboxHandler<RetryOutboxTests.RetryOutboxEvent, RetryOutboxTests.OutboxHandleTestsHandler>();

            services
                .AddOutboxHandler<ParallelWorkersTests.ParallelWorkerEvent, ParallelWorkersTests.OutboxHandleTestsHandler>()
                .ConfigureOptions<ParallelWorkersTests.OutboxHandleTestsHandlerOptions>();

            services
                .AddOutboxHandler<OutboxHandleTests.OutboxHandleTestsEvent, OutboxHandleTests.OutboxHandleTestsHandler>();

            services
                .AddOutboxHandler<DisabledWorkerTests.TestMessage, DisabledWorkerTests.TestMessageHandler>()
                .ConfigureOptions<DisabledWorkerTests.TestMessageHandlerOptions>();

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

            services
                .AddOutboxHandler<CleaningOutboxHandlerTests.Message, CleaningOutboxHandlerTests.Handler>()
                .ConfigureOptions<CleaningRunnerOptions>();
        }

        public void Configure(IApplicationBuilder app)
        {
        }
    }

    private sealed class OutboxConnectionFactory(IConfiguration configuration) : IOutboxConnectionFactory
    {
        public string GetConnectionString()
            => configuration.GetConnectionString("Outbox") ??
               throw new InvalidOperationException("Outbox connection string not configured");

        public Task<DbConnection> GetConnectionAsync(CancellationToken cancellationToken)
            => Task.FromResult<DbConnection>(new MySqlConnection(GetConnectionString()));
    }
}
