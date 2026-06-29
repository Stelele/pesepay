namespace PesePay.Domain;

/// <summary>
/// Payment status returned from <see cref="IPesePayClient.CheckPaymentStatusAsync"/> and <see cref="IPesePayClient.PollTransactionAsync"/>.
/// </summary>
/// <param name="ReferenceNumber">The payment reference number.</param>
/// <param name="PollUrl">URL to poll for payment status updates.</param>
/// <param name="RedirectUrl">Optional redirect URL.</param>
/// <param name="TransactionStatus">Raw transaction status from PesePay (e.g. "SUCCESS", "PENDING", "FAILED").</param>
public record PaymentStatus(string ReferenceNumber, Uri PollUrl, Uri? RedirectUrl, string TransactionStatus)
{
    /// <summary>True if the payment was successful (<see cref="TransactionStatus"/> is "SUCCESS").</summary>
    public bool IsPaid => TransactionStatus == "SUCCESS";
}
