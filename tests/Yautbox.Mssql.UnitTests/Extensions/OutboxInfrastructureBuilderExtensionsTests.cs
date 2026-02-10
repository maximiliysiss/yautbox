using System.Linq;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Yautbox.Extensions.Ioc;
using Yautbox.Mssql.Extensions;
using Yautbox.Mssql.Infrastructure.Database;

namespace Yautbox.Mssql.UnitTests.Extensions;

public class OutboxInfrastructureBuilderExtensionsTests
{
    [Fact]
    public void AddOutbox_ShouldContainsOnlyOneConnectionFactory()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddOutbox(opt => opt.UseMssql(string.Empty));

        // Assert
        services
            .Where(c => c.ServiceType == typeof(IOutboxConnectionFactory))
            .Should().ContainSingle();
    }
}
