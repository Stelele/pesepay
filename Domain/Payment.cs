namespace PesePay.Domain;

public record Payment(
    CurrencyCode CurrencyCode,
    string PaymentMethodCode,
    Customer Customer)
{
    public string? ReferenceNumber { get; set; }
    public Amount? AmountDetails { get; set; }
    public string? ReasonForPayment { get; set; }
    public Dictionary<string, string>? PaymentMethodRequiredFields { get; set; }
    public Dictionary<string, string>? PaymentRequestFields { get; set; }
    public string? MerchantReference { get; set; }
    public string? ReturnUrl { get; set; }
    public string? ResultUrl { get; set; }
}
