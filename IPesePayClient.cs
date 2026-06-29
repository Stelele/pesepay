using PesePay.Domain;

namespace PesePay;

/// <summary>
/// Client for interacting with the PesePay payment gateway.
/// Supports redirect payments, seamless payments, and payment status checking.
/// </summary>
/// <remarks>
/// Use <see cref="ServiceCollectionExtensions.AddPesePay(IServiceCollection, Action{PesePayConfiguration})"/>
/// for ASP.NET Core dependency injection, or construct directly with integration/encryption keys.
/// </remarks>
public interface IPesePayClient
{
    /// <summary>
    /// The URL PesePay will POST the payment result to (required for initiating transactions).
    /// </summary>
    string? ResultUrl { get; set; }

    /// <summary>
    /// The URL the customer will be redirected back to after payment completion.
    /// </summary>
    string? ReturnUrl { get; set; }

    /// <summary>
    /// Creates a <see cref="Transaction"/> object for a redirect payment.
    /// </summary>
    /// <param name="amount">The payment amount.</param>
    /// <param name="currency">Currency code (USD or ZWL).</param>
    /// <param name="reason">Reason for the payment.</param>
    /// <param name="merchantRef">Optional merchant reference.</param>
    Transaction CreateTransaction(decimal amount, CurrencyCode currency, string reason, string? merchantRef = null);

    /// <summary>
    /// Creates a <see cref="Payment"/> object for a seamless payment.
    /// </summary>
    /// <param name="currency">Currency code (USD or ZWL).</param>
    /// <param name="methodCode">Payment method code (e.g. "ecocash").</param>
    /// <param name="email">Customer email (required if phone is null).</param>
    /// <param name="phone">Customer phone number (required if email is null).</param>
    /// <param name="name">Customer name (optional).</param>
    Payment CreatePayment(CurrencyCode currency, string methodCode, string? email, string? phone, string? name);

    /// <summary>
    /// Initiates a redirect transaction. Sets <see cref="ResultUrl"/> and <see cref="ReturnUrl"/> before calling.
    /// </summary>
    /// <param name="transaction">The transaction to initiate.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the redirect URL, reference number, and poll URL on success.</returns>
    Task<PesepayResult<InitiateResponse>> InitiateTransactionAsync(Transaction transaction, CancellationToken ct = default);

    /// <summary>
    /// Makes a seamless (server-to-server) payment. Sets <see cref="ResultUrl"/> before calling.
    /// </summary>
    /// <param name="payment">The payment to process.</param>
    /// <param name="reason">Reason for the payment.</param>
    /// <param name="amount">The payment amount.</param>
    /// <param name="merchantReference">Your merchant reference for this transaction (required).</param>
    /// <param name="fields">Optional method-specific required fields.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the reference number, poll URL, and payment status on success.</returns>
    Task<PesepayResult<PaymentResponse>> MakeSeamlessPaymentAsync(Payment payment, string reason, decimal amount, string merchantReference, Dictionary<string, string>? fields = null, CancellationToken ct = default);

    /// <summary>
    /// Checks the status of a payment by its reference number.
    /// </summary>
    /// <param name="referenceNumber">The payment reference number.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<PesepayResult<PaymentStatus>> CheckPaymentStatusAsync(string referenceNumber, CancellationToken ct = default);

    /// <summary>
    /// Checks the status of a payment by its poll URL (returned from <see cref="InitiateTransactionAsync"/> or <see cref="MakeSeamlessPaymentAsync"/>).
    /// </summary>
    /// <param name="pollUrl">The poll URL from the payment response.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<PesepayResult<PaymentStatus>> PollTransactionAsync(Uri pollUrl, CancellationToken ct = default);

    /// <summary>
    /// Gets the currently active currencies on the PesePay gateway.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    Task<PesepayResult<List<CurrencyInfo>>> GetActiveCurrenciesAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets payment methods available for a given currency.
    /// </summary>
    /// <param name="currencyCode">The currency code (e.g. "USD" or "ZWL").</param>
    /// <param name="ct">Cancellation token.</param>
    Task<PesepayResult<List<PaymentMethodInfo>>> GetPaymentMethodsAsync(string currencyCode, CancellationToken ct = default);
}
