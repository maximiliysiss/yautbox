using System.Linq;
using System.Text.Json;
using FluentAssertions;
using FluentAssertions.Json;
using FluentAssertions.Primitives;
using Newtonsoft.Json.Linq;

namespace Yautbox.Mssql.IntegrationTests.Shared.Extensions;

public static class StringAssertionsExtensions
{
    public static AndConstraint<JTokenAssertions> BeJsonEquivalentTo(this StringAssertions assertions, string expected)
        => JToken.Parse(assertions.Subject).Normalize().Should().BeEquivalentTo(JToken.Parse(expected).Normalize());

    public static AndConstraint<JTokenAssertions> NotBeJsonEquivalentTo(this StringAssertions assertions, string expected)
        => JToken.Parse(assertions.Subject).Normalize().Should().NotBeEquivalentTo(JToken.Parse(expected).Normalize());

    public static AndConstraint<JTokenAssertions> BeJsonEquivalentTo(this StringAssertions assertions, object expected)
        => assertions.BeJsonEquivalentTo(JsonSerializer.Serialize(expected));

    private static JToken Normalize(this JToken token)
    {
        return token switch
        {
            JObject obj => new JObject(obj.Properties().OrderBy(p => p.Name).Select(p => new JProperty(p.Name, p.Value.Normalize()))),
            JArray array => new JArray(array.Select(Normalize).OrderBy(x => x.ToString(Newtonsoft.Json.Formatting.None))),
            _ => token
        };
    }
}
