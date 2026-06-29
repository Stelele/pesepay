using System.Text.Json;
using System.Text.Json.Serialization;

namespace PesePay.Domain.Tests;

public class PaymentTests
{
    private static JsonSerializerOptions ApiOptions => new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower) },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    [Fact]
    public void Payment_Created_With_Required_Fields()
    {
        var customer = new Customer("a@b.com", null, null);
        var payment = new Payment(CurrencyCode.Usd, "ecocash", customer);

        Assert.Equal(CurrencyCode.Usd, payment.CurrencyCode);
        Assert.Equal("ecocash", payment.PaymentMethodCode);
        Assert.Equal(customer, payment.Customer);
    }

    [Fact]
    public void Payment_Serializes_Correctly()
    {
        var customer = new Customer("a@b.com", "123", "John");
        var payment = new Payment(CurrencyCode.Zwl, "ecocash", customer)
        {
            ReasonForPayment = "Invoice #123",
            AmountDetails = new Amount(500m, CurrencyCode.Zwl),
            ResultUrl = "https://ex.com/result",
            ReturnUrl = "https://ex.com/return",
            PaymentMethodRequiredFields = new Dictionary<string, string> { { "field1", "value1" } },
            PaymentRequestFields = new Dictionary<string, string> { { "field1", "value1" } }
        };

        var json = JsonSerializer.Serialize(payment, ApiOptions);

        Assert.Contains("\"currency_code\":\"zwl\"", json);
        Assert.Contains("\"payment_method_code\":\"ecocash\"", json);
        Assert.Contains("\"reason_for_payment\":\"Invoice #123\"", json);
        Assert.Contains("\"customer\"", json);
        Assert.Contains("\"amount_details\"", json);
        Assert.Contains("\"payment_method_required_fields\"", json);
        Assert.Contains("\"payment_request_fields\"", json);
    }

    [Fact]
    public void Payment_Optional_Fields_Omitted_When_Null()
    {
        var customer = new Customer("a@b.com", null, null);
        var payment = new Payment(CurrencyCode.Usd, "ecocash", customer);

        var json = JsonSerializer.Serialize(payment, ApiOptions);

        Assert.DoesNotContain("reason_for_payment", json);
        Assert.DoesNotContain("amount_details", json);
        Assert.DoesNotContain("result_url", json);
        Assert.DoesNotContain("return_url", json);
        Assert.DoesNotContain("payment_method_required_fields", json);
        Assert.DoesNotContain("payment_request_fields", json);
    }
}
