using System.Text.Json;
using System.Text.Json.Serialization;

namespace PesePay.Domain.Tests;

public class AmountTests
{
    [Fact]
    public void Amount_Serializes_With_SnakeCase()
    {
        var amount = new Amount(10.50m, CurrencyCode.Usd);
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower) }
        };

        var json = JsonSerializer.Serialize(amount, options);

        Assert.Contains("\"value\":10.50", json);
        Assert.Contains("\"currency\":\"usd\"", json);
    }

    [Fact]
    public void Amount_Equality_Is_Value_Based()
    {
        var a1 = new Amount(10m, CurrencyCode.Usd);
        var a2 = new Amount(10m, CurrencyCode.Usd);
        var a3 = new Amount(20m, CurrencyCode.Usd);

        Assert.Equal(a1, a2);
        Assert.NotEqual(a1, a3);
    }
}
