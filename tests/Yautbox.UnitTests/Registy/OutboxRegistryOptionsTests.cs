using FluentAssertions;
using NSubstitute;
using Xunit;
using Yautbox.Exceptions;
using Yautbox.Extensions.Types;
using Yautbox.Registy;
using Yautbox.Runner.Options;

namespace Yautbox.UnitTests.Registy;

public class OutboxRegistryOptionsTests
{
    [Fact]
    public void Register_ShouldStoreCustomIdentifier()
    {
        // Arrange
        var options = new OutboxRegistryOptions();
        var monitor = Substitute.For<Microsoft.Extensions.Options.IOptionsMonitor<IOutboxRunnerOptions>>();
        monitor.CurrentValue.Returns(new InnerDefaultRunnerOptions { Identifier = "custom-id" });

        // Act
        options.Register<Message>(monitor);

        // Assert
        options.Identifiers[typeof(Message)].Should().Be("custom-id");
    }

    [Fact]
    public void Register_ShouldFallbackToVersionFreeName_WhenIdentifierIsNull()
    {
        // Arrange
        var options = new OutboxRegistryOptions();
        var monitor = Substitute.For<Microsoft.Extensions.Options.IOptionsMonitor<IOutboxRunnerOptions>>();
        monitor.CurrentValue.Returns(new InnerDefaultRunnerOptions { Identifier = null });
        var expected = typeof(Message).GetVersionFreeFullName();

        // Act
        options.Register<Message>(monitor);

        // Assert
        options.Identifiers[typeof(Message)].Should().Be(expected);
    }

    [Fact]
    public void Register_ShouldThrow_WhenHandlerAlreadyAdded()
    {
        // Arrange
        var options = new OutboxRegistryOptions();
        var monitor = Substitute.For<Microsoft.Extensions.Options.IOptionsMonitor<IOutboxRunnerOptions>>();
        monitor.CurrentValue.Returns(new InnerDefaultRunnerOptions { Identifier = "custom-id" });

        // Act
        var act = () =>
        {
            options.Register<Message>(monitor);
            options.Register<Message>(monitor);
        };

        // Assert
        act.Should().Throw<HandlerAlreadyAddedException>();
    }

    private sealed class Message;
}
