using System;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Xunit;
using Yautbox.Extensions.Types;
using Yautbox.Registy;

namespace Yautbox.UnitTests.Registy;

public class OutboxRegistryTests
{
    [Fact]
    public void GetIdentifier_ShouldReturnConfiguredIdentifier()
    {
        // Arrange
        var options = CreateOptions(o => o.Identifiers[typeof(Message)] = "custom-id");
        var registry = new OutboxRegistry(options);

        // Act
        var result = registry.GetIdentifier<Message>();

        // Assert
        result.Should().Be("custom-id");
    }

    [Fact]
    public void GetIdentifier_ShouldFallbackToVersionFreeName_WhenNotConfigured()
    {
        // Arrange
        var options = CreateOptions(_ => { });
        var registry = new OutboxRegistry(options);
        var expected = typeof(Message).GetVersionFreeFullName();

        // Act
        var result = registry.GetIdentifier<Message>();

        // Assert
        result.Should().Be(expected);
    }

    private static IOptionsSnapshot<OutboxRegistryOptions> CreateOptions(Action<OutboxRegistryOptions> configure)
    {
        var setups = new IConfigureOptions<OutboxRegistryOptions>[]
        {
            new ConfigureNamedOptions<OutboxRegistryOptions>(Options.DefaultName, configure),
        };

        var factory = new OptionsFactory<OutboxRegistryOptions>(setups, Array.Empty<IPostConfigureOptions<OutboxRegistryOptions>>());
        return new OptionsManager<OutboxRegistryOptions>(factory);
    }

    private sealed class Message;
}
