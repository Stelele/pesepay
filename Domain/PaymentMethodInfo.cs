using System.Text.Json.Serialization;

namespace PesePay.Domain;

/// <summary>
/// Payment method information returned by the Get Payment Methods API.
/// </summary>
public record PaymentMethodInfo(
    [property: JsonPropertyName("active")] bool IsActive,
    string Code,
    List<string> Currencies,
    string Description,
    int Id,
    [property: JsonPropertyName("maximumAmount")] decimal MaximumAmount,
    [property: JsonPropertyName("minimumAmount")] decimal MinimumAmount,
    string Name,
    string ProcessingPaymentMessage,
    [property: JsonPropertyName("redirectRequired")] bool RedirectRequired,
    string RedirectURL,
    List<PaymentMethodRequiredField> RequiredFields);

/// <summary>
/// A required field for a payment method.
/// </summary>
public record PaymentMethodRequiredField(
    string DisplayName,
    string FieldType,
    string Name,
    [property: JsonPropertyName("optional")] bool IsOptional);
