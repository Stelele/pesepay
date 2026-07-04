using System.Text.Json.Serialization;

namespace PesePay.Domain;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PaymentMethodCode
{
    EcoCash,
    InnBucks,
    Visa,
    MasterCard,
    Zimswitch,
    Omari,
    PayGo,
}
