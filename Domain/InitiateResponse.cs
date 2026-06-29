namespace PesePay.Domain;

/// <summary>
/// Response from <see cref="IPesePayClient.InitiateTransactionAsync"/>.
/// </summary>
/// <param name="ReferenceNumber">The payment reference number for status checking.</param>
/// <param name="PollUrl">URL to poll for payment status updates.</param>
/// <param name="RedirectUrl">URL to redirect the customer to complete payment.</param>
/// <param name="InternalReference">PesePay's internal transaction reference.</param>
/// <param name="TransactionStatus">Current transaction status (e.g. "INITIATED").</param>
/// <param name="TransactionStatusCode">Numeric status code.</param>
/// <param name="TransactionStatusDescription">Human-readable status description.</param>
public record InitiateResponse(
    string ReferenceNumber,
    Uri PollUrl,
    Uri RedirectUrl,
    string InternalReference,
    string TransactionStatus,
    int TransactionStatusCode,
    string TransactionStatusDescription);
