namespace PesePay.Domain;

public record PaymentStatus(string ReferenceNumber, Uri PollUrl, Uri? RedirectUrl, string TransactionStatus)
{
    public bool IsPaid => TransactionStatus == "SUCCESS";
}
