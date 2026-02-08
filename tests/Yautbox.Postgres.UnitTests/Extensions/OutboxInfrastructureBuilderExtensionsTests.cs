using System.Linq;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Yautbox.Extensions.Ioc;
using Yautbox.Postgres.Extensions;
using Yautbox.Postgres.Infrastructure.Database;

namespace Yautbox.Postgres.UnitTests.Extensions;

public class OutboxInfrastructureBuilderExtensionsTests
{
    [Fact]
    public void AddOutbox_ShouldContainsOnlyOneConnectionFactory()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddOutbox(opt => opt.UsePostgres(string.Empty));

        // Assert
        services
            .Where(c => c.ServiceType == typeof(IOutboxConnectionFactory))
            .Should().ContainSingle();
    }
}
