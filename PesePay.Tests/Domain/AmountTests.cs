using System.Text.Json;
using System.Text.Json.Serialization;

namespace PesePay.Domain.Tests;

public class AmountTests
{
    [Fact]
    public void Amount_Serializes_With_SnakeCase()
    {
        var amount = new Amount(10.50m, CurrencyCode.USD);
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower) }
        };

        var json = JsonSerializer.Serialize(amount, options);

        Assert.Contains("\"amount\":10.50", json);
        Assert.Contains("\"currencyCode\":\"usd\"", json);
    }

    [Fact]
    public void Amount_Equality_Is_Value_Based()
    {
        var a1 = new Amount(10m, CurrencyCode.USD);
        var a2 = new Amount(10m, CurrencyCode.USD);
        var a3 = new Amount(20m, CurrencyCode.USD);

        Assert.Equal(a1, a2);
        Assert.NotEqual(a1, a3);
    }
}
