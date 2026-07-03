using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace PesePay.Domain;

/// <summary>
/// Supported currencies for PesePay payments.
/// PesePay currently supports USD and ZWL only.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CurrencyCode
{
    /// <summary>United States Dollar</summary>
    [EnumMember(Value = "USD")]
    USD,

    /// <summary>Zimbabwe Dollar (ZWL)</summary>
    [EnumMember(Value = "ZWL")]
    ZWL
}
