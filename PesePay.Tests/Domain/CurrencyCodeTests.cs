using System.Text.Json;
using System.Text.Json.Serialization;

namespace PesePay.Domain.Tests;

public class CurrencyCodeTests
{
    [Fact]
    public void CurrencyCode_Serializes_To_Correct_String()
    {
        var options = new JsonSerializerOptions
        {
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower) }
        };

        var json = JsonSerializer.Serialize(CurrencyCode.Usd, options);

        Assert.Equal("\"usd\"", json);
    }

    [Fact]
    public void CurrencyCode_Zwl_Serializes_To_zwl()
    {
        var options = new JsonSerializerOptions
        {
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower) }
        };

        var json = JsonSerializer.Serialize(CurrencyCode.Zwl, options);

        Assert.Equal("\"zwl\"", json);
    }

    [Fact]
    public void CurrencyCode_Deserializes_From_String()
    {
        var options = new JsonSerializerOptions
        {
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower) }
        };

        var result = JsonSerializer.Deserialize<CurrencyCode>("\"zwl\"", options);

        Assert.Equal(CurrencyCode.Zwl, result);
    }
}
