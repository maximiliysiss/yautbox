using System;
using FluentAssertions;
using Xunit;
using Yautbox.Extensions.DateTime;

namespace Yautbox.UnitTests.Extensions;

public class TimeSpanExtensionsTests
{
    [Fact]
    public void Jitter_ShouldReturnValueWithinRange()
    {
        // Arrange
        var baseSpan = TimeSpan.FromSeconds(1);
        const int min = 10;
        const int max = 20;

        // Act
        var result = baseSpan.Jitter(min, max);

        // Assert
        result.Should().BeGreaterThanOrEqualTo(baseSpan + TimeSpan.FromMilliseconds(min));
        result.Should().BeLessThan(baseSpan + TimeSpan.FromMilliseconds(max));
    }
}
