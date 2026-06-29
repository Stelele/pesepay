using PesePay.Domain;

namespace PesePay;

public interface IPesePayClient
{
    string? ResultUrl { get; set; }
    string? ReturnUrl { get; set; }

    Transaction CreateTransaction(decimal amount, CurrencyCode currency, string reason, string? merchantRef = null);
    Payment CreatePayment(CurrencyCode currency, string methodCode, string? email, string? phone, string? name);

    Task<PesepayResult<InitiateResponse>> InitiateTransactionAsync(Transaction transaction, CancellationToken ct = default);
    Task<PesepayResult<PaymentResponse>> MakeSeamlessPaymentAsync(Payment payment, string reason, decimal amount, Dictionary<string, string>? fields = null, CancellationToken ct = default);
    Task<PesepayResult<PaymentStatus>> CheckPaymentStatusAsync(string referenceNumber, CancellationToken ct = default);
    Task<PesepayResult<PaymentStatus>> PollTransactionAsync(Uri pollUrl, CancellationToken ct = default);
}
