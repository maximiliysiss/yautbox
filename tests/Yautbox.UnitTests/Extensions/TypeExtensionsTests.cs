using System;
using FluentAssertions;
using Xunit;
using Yautbox.Extensions.Types;

namespace Yautbox.UnitTests.Extensions;

public class TypeExtensionsTests
{
    [Fact]
    public void GetVersionFreeFullName_ShouldReturnFullNameAndAssemblyName()
    {
        // Arrange
        var type = typeof(Message);
        var expected = $"{type.FullName}, {type.Assembly.GetName().Name}";

        // Act
        var result = type.GetVersionFreeFullName();

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void GetVersionFreeFullName_ShouldThrow_WhenTypeIsNull()
    {
        // Arrange
        Type? type = null;

        // Act
        Action act = () => type!.GetVersionFreeFullName();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    private sealed class Message;
}
