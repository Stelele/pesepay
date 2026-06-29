namespace PesePay.Domain;

/// <summary>
/// Response from <see cref="IPesePayClient.MakeSeamlessPaymentAsync"/>.
/// </summary>
/// <param name="ReferenceNumber">The payment reference number for status checking.</param>
/// <param name="PollUrl">URL to poll for payment status updates.</param>
/// <param name="RedirectUrl">Optional redirect URL (usually null for seamless payments).</param>
/// <param name="InternalReference">PesePay's internal transaction reference.</param>
/// <param name="TransactionStatus">Raw transaction status from PesePay (e.g. "SUCCESS", "PENDING").</param>
/// <param name="TransactionStatusCode">Numeric status code.</param>
/// <param name="TransactionStatusDescription">Human-readable status description.</param>
public record PaymentResponse(
    string ReferenceNumber,
    Uri PollUrl,
    Uri? RedirectUrl,
    string InternalReference,
    string TransactionStatus,
    int TransactionStatusCode,
    string TransactionStatusDescription)
{
    /// <summary>True if the payment was successful (<see cref="TransactionStatus"/> is "SUCCESS").</summary>
    public bool IsPaid => TransactionStatus == "SUCCESS";
}
