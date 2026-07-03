using System.Text.Json;
using System.Text.Json.Serialization;

namespace PesePay.Domain.Tests;

public class TransactionTests
{
    private static JsonSerializerOptions ApiOptions => new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower) },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    [Fact]
    public void Transaction_Default_Type_Is_Basic()
    {
        var txn = new Transaction(
            new Amount(10m, CurrencyCode.USD),
            "Test payment");

        Assert.Equal(TransactionType.Basic, txn.Type);
    }

    [Fact]
    public void Transaction_Serializes_With_SnakeCase()
    {
        var txn = new Transaction(
            new Amount(10m, CurrencyCode.USD),
            "Test payment",
            "MERCH001");

        var json = JsonSerializer.Serialize(txn, ApiOptions);

        Assert.Contains("\"amount_details\"", json);
        Assert.Contains("\"currency\":\"usd\"", json);
        Assert.Contains("\"reason_for_payment\":\"Test payment\"", json);
        Assert.Contains("\"merchant_reference\":\"MERCH001\"", json);
        Assert.Contains("\"transaction_type\":\"basic\"", json);
    }

    [Fact]
    public void Transaction_ResultUrl_And_ReturnUrl_Settable()
    {
        var txn = new Transaction(
            new Amount(10m, CurrencyCode.USD),
            "Test");

        txn.ResultUrl = "https://example.com/result";
        txn.ReturnUrl = "https://example.com/return";

        Assert.Equal("https://example.com/result", txn.ResultUrl);
        Assert.Equal("https://example.com/return", txn.ReturnUrl);
    }

    [Fact]
    public void Transaction_ResultUrl_Serializes_When_Set()
    {
        var txn = new Transaction(
            new Amount(10m, CurrencyCode.USD),
            "Test")
        {
            ResultUrl = "https://example.com/result",
            ReturnUrl = "https://example.com/return"
        };

        var json = JsonSerializer.Serialize(txn, ApiOptions);

        Assert.Contains("\"result_url\":\"https://example.com/result\"", json);
        Assert.Contains("\"return_url\":\"https://example.com/return\"", json);
    }
}
