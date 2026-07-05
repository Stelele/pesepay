namespace PesePay.Domain;

/// <summary>
/// Request to initiate a seamless (server-to-server) payment.
/// Set <see cref="Card"/> for card payments or <see cref="PhoneNumber"/> for mobile money.
/// If both are set, <see cref="Card"/> takes priority.
/// </summary>
/// <param name="Method">The payment method (e.g. EcoCash, Visa).</param>
/// <param name="Currency">Currency code (USD or ZWL).</param>
/// <param name="Amount">The payment amount.</param>
/// <param name="Reason">Reason for the payment.</param>
/// <param name="MerchantReference">Your merchant reference for this transaction (required).</param>
/// <param name="Email">Customer email.</param>
/// <param name="CustomerName">Optional customer name.</param>
/// <param name="PhoneNumber">Customer phone number for mobile money payments.</param>
/// <param name="Card">Card details for card payments.</param>
public record SeamlessPaymentRequest(
    PaymentMethodCode Method,
    CurrencyCode Currency,
    decimal Amount,
    string Reason,
    string MerchantReference,
    string? Email = null,
    string? CustomerName = null,
    string? PhoneNumber = null,
    CardDetails? Card = null);
