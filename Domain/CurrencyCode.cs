using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace PesePay.Domain;

/// <summary>
/// Supported currencies for PesePay payments.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CurrencyCode
{
    /// <summary>United States Dollar</summary>
    [EnumMember(Value = "USD")]
    USD,

    /// <summary>Zimbabwe Gold</summary>
    [EnumMember(Value = "ZiG")]
    ZiG,

    /// <summary>Zimbabwe Dollar (deprecated — replaced by ZiG)</summary>
    [EnumMember(Value = "ZWL")]
    ZWL
}
