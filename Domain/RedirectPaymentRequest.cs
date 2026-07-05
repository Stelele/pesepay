namespace PesePay.Domain;

/// <summary>
/// Request to initiate a redirect payment where the customer is sent to
/// a PesePay-hosted payment page to complete the transaction.
/// </summary>
/// <param name="Amount">The payment amount.</param>
/// <param name="Currency">Currency code (USD or ZWL).</param>
/// <param name="Reason">Reason for the payment.</param>
/// <param name="MerchantReference">Optional merchant reference.</param>
public record RedirectPaymentRequest(
    decimal Amount,
    CurrencyCode Currency,
    string Reason,
    string? MerchantReference = null);
