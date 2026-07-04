using System.Text.Json;
using System.Text.Json.Serialization;

namespace PesePay.Domain.Tests;

public class CurrencyCodeTests
{
    [Fact]
    public void CurrencyCode_Serializes_As_Member_Name()
    {
        var options = new JsonSerializerOptions
        {
            Converters = { new JsonStringEnumConverter() }
        };

        var json = JsonSerializer.Serialize(CurrencyCode.USD, options);

        Assert.Equal("\"USD\"", json);
    }

    [Fact]
    public void CurrencyCode_ZWL_Serializes_As_ZWL()
    {
        var options = new JsonSerializerOptions
        {
            Converters = { new JsonStringEnumConverter() }
        };

        var json = JsonSerializer.Serialize(CurrencyCode.ZWL, options);

        Assert.Equal("\"ZWL\"", json);
    }

    [Fact]
    public void CurrencyCode_ZiG_Serializes_As_ZiG()
    {
        var options = new JsonSerializerOptions
        {
            Converters = { new JsonStringEnumConverter() }
        };

        var json = JsonSerializer.Serialize(CurrencyCode.ZiG, options);

        Assert.Equal("\"ZiG\"", json);
    }

    [Fact]
    public void CurrencyCode_Deserializes_From_String()
    {
        var options = new JsonSerializerOptions
        {
            Converters = { new JsonStringEnumConverter() }
        };

        var result = JsonSerializer.Deserialize<CurrencyCode>("\"ZWL\"", options);

        Assert.Equal(CurrencyCode.ZWL, result);
    }

    [Fact]
    public void CurrencyCode_ZiG_Deserializes_From_String()
    {
        var options = new JsonSerializerOptions
        {
            Converters = { new JsonStringEnumConverter() }
        };

        var result = JsonSerializer.Deserialize<CurrencyCode>("\"ZiG\"", options);

        Assert.Equal(CurrencyCode.ZiG, result);
    }
}
