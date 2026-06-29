using System.Text.Json;
using System.Text.Json.Serialization;

namespace PesePay.Domain.Tests;

public class EnvironmentTypeTests
{
    [Fact]
    public void EnvironmentType_Has_Sandbox_And_Production()
    {
        var values = Enum.GetValues<EnvironmentType>();
        Assert.Contains(EnvironmentType.Sandbox, values);
        Assert.Contains(EnvironmentType.Production, values);
        Assert.Equal(2, values.Length);
    }

    [Fact]
    public void EnvironmentType_Serializes_As_String()
    {
        var options = new JsonSerializerOptions
        {
            Converters = { new JsonStringEnumConverter() }
        };

        var json = JsonSerializer.Serialize(EnvironmentType.Sandbox, options);

        Assert.Equal("\"Sandbox\"", json);
    }
}
