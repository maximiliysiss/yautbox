using System;
using System.Data.Common;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Yautbox.Extensions.Ioc;
using Yautbox.Postgres.Extensions;
using Yautbox.Postgres.Infrastructure.Database;
using Yautbox.Postgres.IntegrationTests.Cases;
using Yautbox.Postgres.IntegrationTests.DbHelper;
using Yautbox.Postgres.IntegrationTests.DbHelper.Repositories;
using Yautbox.Postgres.IntegrationTests.Shared.Options;
using Yautbox.Postgres.Repositories;
using Yautbox.Registy;

namespace Yautbox.Postgres.IntegrationTests.Shared.Fixture;

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
                    .UsePostgres<OutboxConnectionFactory>()
                    .SetRegistryPolicy(OutboxRegistryPolicy.Strict)
                    .SetPrefix(version));

            services
                .Decorate<IPostgresOutboxRepository, TrackingOutboxRepository>();

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
        private readonly NpgsqlDataSource _dataSource = new NpgsqlDataSourceBuilder(configuration.GetConnectionString("Outbox")).Build();

        public string GetConnectionString() => _dataSource.ConnectionString;

        public Task<DbConnection> GetConnectionAsync(CancellationToken cancellationToken)
            => Task.FromResult<DbConnection>(_dataSource.CreateConnection());
    }
}
