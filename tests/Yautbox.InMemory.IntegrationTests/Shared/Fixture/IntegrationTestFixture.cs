using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Yautbox.Extensions.Ioc;
using Yautbox.InMemory.Extensions;
using Yautbox.InMemory.IntegrationTests.Cases;

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
                .AddOutboxHandler<SimpleOutboxHandlerTests.Message, SimpleOutboxHandlerTests.Handler>();
        }

        public void Configure(IApplicationBuilder app)
        {
        }
    }
}
