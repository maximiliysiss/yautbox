using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Yautbox.Extensions.Ioc;
using Yautbox.InMemory.Extensions;

namespace Yautbox.InMemory.IntegrationTests.Shared.Fixture;

public sealed class IntegrationTestFixture : WebApplicationFactory<IntegrationTestFixture.Startup>
{
    public sealed class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddOutbox(b => b.UseInMemory());
        }

        public void Configure(IApplicationBuilder app)
        {
        }
    }
}
