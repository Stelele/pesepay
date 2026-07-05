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
    /// Initiates a redirect transaction where the customer is sent to a
    /// PesePay-hosted payment page.
    /// </summary>
    /// <param name="request">The redirect payment request details.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the redirect URL, reference number, and poll URL on success.</returns>
    Task<PesepayResult<InitiateResponse>> InitiateRedirectPaymentAsync(
        RedirectPaymentRequest request, CancellationToken ct = default);

    /// <summary>
    /// Makes a seamless (server-to-server) payment for mobile money or card.
    /// Set <see cref="SeamlessPaymentRequest.Card"/> for card payments or
    /// <see cref="SeamlessPaymentRequest.PhoneNumber"/> for mobile money.
    /// </summary>
    /// <param name="request">The seamless payment request details.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the reference number, poll URL, and payment status on success.</returns>
    Task<PesepayResult<PaymentResponse>> InitiateSeamlessPaymentAsync(
        SeamlessPaymentRequest request, CancellationToken ct = default);

    /// <summary>
    /// Checks the status of a payment by its reference number.
    /// </summary>
    /// <param name="referenceNumber">The payment reference number.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<PesepayResult<PaymentStatus>> CheckPaymentStatusAsync(
        string referenceNumber, CancellationToken ct = default);

    /// <summary>
    /// Checks the status of a payment by its poll URL.
    /// </summary>
    /// <param name="pollUrl">The poll URL from the payment response.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<PesepayResult<PaymentStatus>> PollPaymentAsync(
        Uri pollUrl, CancellationToken ct = default);

    /// <summary>
    /// Gets the currently active currencies on the PesePay gateway.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    Task<PesepayResult<List<CurrencyInfo>>> GetActiveCurrenciesAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Gets payment methods available for a given currency.
    /// </summary>
    /// <param name="currencyCode">The currency code (e.g. "USD" or "ZWL").</param>
    /// <param name="ct">Cancellation token.</param>
    Task<PesepayResult<List<PaymentMethodInfo>>> GetPaymentMethodsAsync(
        string currencyCode, CancellationToken ct = default);
}
