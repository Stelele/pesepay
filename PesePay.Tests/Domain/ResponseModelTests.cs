using System.Text.Json;
using System.Text.Json.Serialization;

namespace PesePay.Domain.Tests;

public class ResponseModelTests
{
    private static JsonSerializerOptions ApiOptions => new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    [Fact]
    public void InitiateResponse_Parses_From_Api_Json()
    {
        var json = """{"reference_number":"REF123","poll_url":"https://api.pesepay.com/poll/REF123","redirect_url":"https://checkout.pesepay.com/pay/REF123","internal_reference":"INT-123","transaction_status":"INITIATED","transaction_status_code":0,"transaction_status_description":"Transaction initiated successfully"}""";

        var response = JsonSerializer.Deserialize<InitiateResponse>(json, ApiOptions);

        Assert.NotNull(response);
        Assert.Equal("REF123", response.ReferenceNumber);
        Assert.Equal("https://api.pesepay.com/poll/REF123", response.PollUrl.ToString());
        Assert.Equal("https://checkout.pesepay.com/pay/REF123", response.RedirectUrl.ToString());
        Assert.Equal("INT-123", response.InternalReference);
        Assert.Equal("INITIATED", response.TransactionStatus);
        Assert.Equal(0, response.TransactionStatusCode);
    }

    [Fact]
    public void PaymentResponse_Parses_From_Api_Json()
    {
        var json = """{"reference_number":"REF456","poll_url":"https://api.pesepay.com/poll/REF456","redirect_url":null,"internal_reference":"INT-456","transaction_status":"SUCCESS","transaction_status_code":1,"transaction_status_description":"Payment successful"}""";

        var response = JsonSerializer.Deserialize<PaymentResponse>(json, ApiOptions);

        Assert.NotNull(response);
        Assert.Equal("REF456", response.ReferenceNumber);
        Assert.Equal("SUCCESS", response.TransactionStatus);
        Assert.Null(response.RedirectUrl);
        Assert.Equal("INT-456", response.InternalReference);
        Assert.True(response.IsPaid);
    }

    [Fact]
    public void PaymentStatus_Parses_From_Api_Json_NotPaid()
    {
        var json = """{"reference_number":"REF789","poll_url":"https://api.pesepay.com/poll/REF789","redirect_url":null,"internal_reference":"INT-789","transaction_status":"PENDING","transaction_status_code":2,"transaction_status_description":"Payment pending"}""";

        var response = JsonSerializer.Deserialize<PaymentStatus>(json, ApiOptions);

        Assert.NotNull(response);
        Assert.False(response.IsPaid);
    }

    [Fact]
    public void IsPaid_False_When_Not_Success()
    {
        var status = new PaymentStatus("R1", new Uri("http://example.com"), null, "INT-1", "FAILED", 3, "Payment failed");
        Assert.False(status.IsPaid);
    }
}
