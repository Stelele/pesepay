namespace PesePay.Domain;

public record InitiateResponse(string ReferenceNumber, Uri PollUrl, Uri RedirectUrl);
