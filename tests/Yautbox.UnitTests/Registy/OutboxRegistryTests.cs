using System;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Xunit;
using Yautbox.Exceptions;
using Yautbox.Extensions.Types;
using Yautbox.Registy;
using Yautbox.Runner.Options;

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

    [Fact]
    public void GetIdentifier_ShouldApplyPrefix_WhenConfigured()
    {
        // Arrange
        var options = CreateOptions(o =>
        {
            o.Prefix = "prefix-";
            o.Identifiers[typeof(Message)] = "custom-id";
        });
        var registry = new OutboxRegistry(options);

        // Act
        var result = registry.GetIdentifier<Message>();

        // Assert
        result.Should().Be("prefix-custom-id");
    }

    [Fact]
    public void GetIdentifier_ShouldThrow_WhenStrictModeAndNotRegistered()
    {
        // Arrange
        var options = CreateOptions(o => o.Policy = OutboxRegistryPolicy.Strict);
        var registry = new OutboxRegistry(options);

        // Act
        var act = () => registry.GetIdentifier<Message>();

        // Assert
        act.Should().Throw<RegistryStrictException>();
    }

    [Fact]
    public void GetCancellationPolicy_ShouldReturnConfiguredPolicy()
    {
        // Arrange
        var options = CreateOptions(o => o.CancellingPolicies[typeof(Message)] = DeletePolicy.Delete);
        var registry = new OutboxRegistry(options);

        // Act
        var result = registry.GetCancellationPolicy<Message>();

        // Assert
        result.Should().Be(DeletePolicy.Delete);
    }

    [Fact]
    public void GetCancellationPolicy_ShouldFallbackToSafe_WhenNotConfigured()
    {
        // Arrange
        var options = CreateOptions(_ => { });
        var registry = new OutboxRegistry(options);

        // Act
        var result = registry.GetCancellationPolicy<Message>();

        // Assert
        result.Should().Be(DeletePolicy.Safe);
    }

    [Fact]
    public void GetCancellationPolicy_ShouldThrow_WhenStrictModeAndNotRegistered()
    {
        // Arrange
        var options = CreateOptions(o => o.Policy = OutboxRegistryPolicy.Strict);
        var registry = new OutboxRegistry(options);

        // Act
        var act = () => registry.GetCancellationPolicy<Message>();

        // Assert
        act.Should().Throw<RegistryStrictException>();
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
