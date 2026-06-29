using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace PesePay.Domain;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CurrencyCode
{
    [EnumMember(Value = "USD")]
    Usd,

    [EnumMember(Value = "ZWL")]
    Zwl
}
