using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Yautbox.Mssql.Environment;

namespace Yautbox.Mssql.UnitTests.Environment;

public class InfrastructureReadinessWaiterTests
{
    [Fact]
    public async Task Waiter_ShouldWork()
    {
        // Arrange
        var waiter = new InfrastructureReadinessWaiter();

        // Act
        await waiter.ReadyAsync(CancellationToken.None);
        await waiter.WaitAsync(CancellationToken.None);

        // Assert
    }
}
