using System.Text.Json.Serialization;

namespace PesePay.Domain;

public record Transaction(
    Amount AmountDetails,
    string ReasonForPayment,
    string? MerchantReference = null,
    [property: JsonPropertyName("transaction_type")]
    TransactionType Type = TransactionType.Basic)
{
    public string? ResultUrl { get; set; }
    public string? ReturnUrl { get; set; }
}
