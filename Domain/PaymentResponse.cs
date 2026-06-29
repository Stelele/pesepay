namespace PesePay.Domain;

public record PaymentResponse(string ReferenceNumber, Uri PollUrl, Uri? RedirectUrl, string TransactionStatus)
{
    public bool IsPaid => TransactionStatus == "SUCCESS";
}
