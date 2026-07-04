namespace PesePay.Domain;

public record CardDetails(
    string Number,
    string Cvv,
    string ExpiryDate,
    string? HolderName = null);
