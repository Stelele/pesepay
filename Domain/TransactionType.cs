using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace PesePay.Domain;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TransactionType
{
    [EnumMember(Value = "BASIC")]
    Basic
}
