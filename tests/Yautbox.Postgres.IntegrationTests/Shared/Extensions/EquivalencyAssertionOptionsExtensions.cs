using System;
using FluentAssertions;
using FluentAssertions.Equivalency;

namespace Yautbox.Postgres.IntegrationTests.Shared.Extensions;

public static class EquivalencyAssertionOptionsExtensions
{
    public static EquivalencyOptions<TExpectation> UsingDateTime<TExpectation>(
        this EquivalencyOptions<TExpectation> options, TimeSpan? precision = null)
    {
        precision ??= TimeSpan.FromSeconds(1);

        return options
            .Using<DateTime>(ctx => ctx.Subject.Should().BeCloseTo(ctx.Expectation, precision.Value))
            .WhenTypeIs<DateTime>()
            .Using<DateTimeOffset>(ctx => ctx.Subject.Should().BeCloseTo(ctx.Expectation, precision.Value))
            .WhenTypeIs<DateTimeOffset>();
    }
}
