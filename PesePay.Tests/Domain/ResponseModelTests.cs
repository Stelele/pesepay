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
        var json = """{"reference_number":"REF123","poll_url":"https://api.pesepay.com/poll/REF123","redirect_url":"https://checkout.pesepay.com/pay/REF123"}""";

        var response = JsonSerializer.Deserialize<InitiateResponse>(json, ApiOptions);

        Assert.NotNull(response);
        Assert.Equal("REF123", response.ReferenceNumber);
        Assert.Equal("https://api.pesepay.com/poll/REF123", response.PollUrl.ToString());
        Assert.Equal("https://checkout.pesepay.com/pay/REF123", response.RedirectUrl.ToString());
    }

    [Fact]
    public void PaymentResponse_Parses_From_Api_Json()
    {
        var json = """{"reference_number":"REF456","poll_url":"https://api.pesepay.com/poll/REF456","redirect_url":null,"transaction_status":"SUCCESS"}""";

        var response = JsonSerializer.Deserialize<PaymentResponse>(json, ApiOptions);

        Assert.NotNull(response);
        Assert.Equal("REF456", response.ReferenceNumber);
        Assert.Equal("SUCCESS", response.TransactionStatus);
        Assert.Null(response.RedirectUrl);
        Assert.True(response.IsPaid);
    }

    [Fact]
    public void PaymentStatus_Parses_From_Api_Json_NotPaid()
    {
        var json = """{"reference_number":"REF789","poll_url":"https://api.pesepay.com/poll/REF789","redirect_url":null,"transaction_status":"PENDING"}""";

        var response = JsonSerializer.Deserialize<PaymentStatus>(json, ApiOptions);

        Assert.NotNull(response);
        Assert.False(response.IsPaid);
    }

    [Fact]
    public void IsPaid_False_When_Not_Success()
    {
        var status = new PaymentStatus("R1", new Uri("http://example.com"), null, "FAILED");
        Assert.False(status.IsPaid);
    }
}
