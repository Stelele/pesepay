using PesePay.Domain;

namespace PesePay.Tests;

public class PesePayClientFactoryTests
{
    [Theory]
    [InlineData(PaymentMethodCode.EcoCash, CurrencyCode.USD, "PZW211")]
    [InlineData(PaymentMethodCode.EcoCash, CurrencyCode.ZiG, "PZW201")]
    [InlineData(PaymentMethodCode.InnBucks, CurrencyCode.USD, "PZW212")]
    [InlineData(PaymentMethodCode.Visa, CurrencyCode.USD, "PZW204")]
    [InlineData(PaymentMethodCode.MasterCard, CurrencyCode.USD, "PZW205")]
    [InlineData(PaymentMethodCode.Zimswitch, CurrencyCode.USD, "PZW215")]
    [InlineData(PaymentMethodCode.Omari, CurrencyCode.USD, "PZW216")]
    [InlineData(PaymentMethodCode.PayGo, CurrencyCode.ZiG, "PZW210")]
    public void GetPaymentMethodCode_Resolves_Correct_Code(
        PaymentMethodCode method, CurrencyCode currency, string expectedCode)
    {
        var code = PesePayClient.GetPaymentMethodCode(method, currency);

        Assert.Equal(expectedCode, code);
    }

    [Fact]
    public void GetPaymentMethodCode_Throws_When_Unsupported_Combination()
    {
        Assert.Throws<PesePayException>(
            () => PesePayClient.GetPaymentMethodCode(PaymentMethodCode.EcoCash, CurrencyCode.ZWL));
    }
}
