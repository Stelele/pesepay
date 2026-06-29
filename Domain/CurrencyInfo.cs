using System.Text.Json.Serialization;

namespace PesePay.Domain;

/// <summary>
/// Currency information returned by the Get Active Currencies API.
/// </summary>
public record CurrencyInfo(
    [property: JsonPropertyName("active")] bool IsActive,
    string Code,
    [property: JsonPropertyName("defaultCurrency")] bool IsDefault,
    string Description,
    int Id,
    string Name);
