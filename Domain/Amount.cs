namespace PesePay.Domain;

/// <summary>
/// Represents a monetary amount with a currency code.
/// </summary>
/// <param name="Value">The decimal amount.</param>
/// <param name="Currency">The currency code.</param>
public readonly record struct Amount(decimal Value, CurrencyCode Currency);
