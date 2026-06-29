namespace PesePay.Domain;

public readonly record struct Amount(decimal Value, CurrencyCode Currency);
