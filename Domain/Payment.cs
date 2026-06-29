namespace PesePay.Domain;

/// <summary>
/// Represents a seamless (server-to-server) payment to be processed by PesePay.
/// </summary>
/// <param name="CurrencyCode">The currency for the payment (USD or ZWL).</param>
/// <param name="PaymentMethodCode">The payment method code (e.g. "ecocash").</param>
/// <param name="Customer">Customer details (email and/or phone number required).</param>
public record Payment(
    CurrencyCode CurrencyCode,
    string PaymentMethodCode,
    Customer Customer)
{
    /// <summary>Optional reference number for this payment.</summary>
    public string? ReferenceNumber { get; set; }

    /// <summary>Payment amount and currency. Set by <see cref="IPesePayClient"/> when sending.</summary>
    public Amount? AmountDetails { get; set; }

    /// <summary>Description of what the payment is for.</summary>
    public string? ReasonForPayment { get; set; }

    /// <summary>Payment-method-specific required fields.</summary>
    public Dictionary<string, string>? PaymentMethodRequiredFields { get; set; }

    /// <summary>Payment request fields (set to same value as <see cref="PaymentMethodRequiredFields"/>).</summary>
    public Dictionary<string, string>? PaymentRequestFields { get; set; }

    /// <summary>Optional merchant reference.</summary>
    public string? MerchantReference { get; set; }

    /// <summary>The URL the customer is returned to after payment. Set by <see cref="IPesePayClient"/>.</summary>
    public string? ReturnUrl { get; set; }

    /// <summary>The URL PesePay posts the payment result to. Set by <see cref="IPesePayClient"/>.</summary>
    public string? ResultUrl { get; set; }
}
