namespace PesePay.Domain;

/// <summary>
/// Represents a redirect payment transaction to be initiated with PesePay.
/// </summary>
/// <param name="AmountDetails">The payment amount and currency.</param>
/// <param name="ReasonForPayment">Description of what the payment is for.</param>
/// <param name="MerchantReference">Optional merchant-defined reference for the transaction.</param>
public record Transaction(
    Amount AmountDetails,
    string ReasonForPayment,
    string? MerchantReference = null)
{
    /// <summary>
    /// The URL PesePay will POST the payment result to. Set by <see cref="IPesePayClient"/> before initiating.
    /// </summary>
    public string? ResultUrl { get; set; }

    /// <summary>
    /// The URL the customer will be returned to after payment. Set by <see cref="IPesePayClient"/> before initiating.
    /// </summary>
    public string? ReturnUrl { get; set; }
}
