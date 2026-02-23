using System.Linq;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Yautbox.Extensions.Ioc;
using Yautbox.Mysql.Extensions;
using Yautbox.Mysql.Infrastructure.Database;

namespace Yautbox.Mysql.UnitTests.Extensions;

public class OutboxInfrastructureBuilderExtensionsTests
{
    [Fact]
    public void AddOutbox_ShouldContainsOnlyOneConnectionFactory()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddOutbox(opt => opt.UseMysql(string.Empty));

        // Assert
        services
            .Where(c => c.ServiceType == typeof(IOutboxConnectionFactory))
            .Should().ContainSingle();
    }
}
