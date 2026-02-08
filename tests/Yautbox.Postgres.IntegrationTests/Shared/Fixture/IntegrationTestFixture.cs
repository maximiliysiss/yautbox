using System;
using System.Data.Common;
using System.IO;
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
            services
                .AddOutbox(b => b.UsePostgres<OutboxConnectionFactory>());
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
            => Task.FromResult<DbConnection>(new NpgsqlConnection(GetConnectionString()));
    }
}
