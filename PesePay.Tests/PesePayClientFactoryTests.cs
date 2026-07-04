using PesePay.Domain;

namespace PesePay.Tests;

public class PesePayClientFactoryTests
{
    private readonly PesePayClient _client = new(
        "integration-key",
        "encryption-key-is-32-chars!!",
        EnvironmentType.Sandbox);

    [Fact]
    public void CreateTransaction_Returns_Transaction_With_Correct_Values()
    {
        var txn = _client.CreateTransaction(100m, CurrencyCode.USD, "Payment for order", "ORDER-001");

        Assert.Equal(100m, txn.AmountDetails.Value);
        Assert.Equal(CurrencyCode.USD, txn.AmountDetails.Currency);
        Assert.Equal("Payment for order", txn.ReasonForPayment);
        Assert.Equal("ORDER-001", txn.MerchantReference);
    }

    [Fact]
    public void CreatePayment_Returns_Payment_With_Correct_Values()
    {
        var payment = _client.CreatePayment(CurrencyCode.ZWL, "ecocash", "a@b.com", "123", "John");

        Assert.Equal(CurrencyCode.ZWL, payment.CurrencyCode);
        Assert.Equal("ecocash", payment.PaymentMethodCode);
        Assert.Equal("a@b.com", payment.Customer.Email);
        Assert.Equal("123", payment.Customer.PhoneNumber);
        Assert.Equal("John", payment.Customer.Name);
    }

    [Fact]
    public void CreatePayment_With_Email_Only()
    {
        var payment = _client.CreatePayment(CurrencyCode.USD, "visa", "a@b.com", null, null);

        Assert.Equal("a@b.com", payment.Customer.Email);
        Assert.Null(payment.Customer.PhoneNumber);
    }

    [Fact]
    public void CreatePayment_With_Enum_Uses_Correct_Code()
    {
        var customer = new Customer("a@b.com", "0771234567", "John");
        var payment = _client.CreatePayment(CurrencyCode.USD, PaymentMethodCode.EcoCash, customer);

        Assert.Equal("PZW211", payment.PaymentMethodCode);
        Assert.Equal(CurrencyCode.USD, payment.CurrencyCode);
        Assert.Equal("John", payment.Customer.Name);
    }

    [Fact]
    public void CreatePayment_With_InnBucks_Enum_Uses_Correct_Code()
    {
        var customer = new Customer(null, "0771234567", "Jane");
        var payment = _client.CreatePayment(CurrencyCode.USD, PaymentMethodCode.InnBucks, customer);

        Assert.Equal("PZW212", payment.PaymentMethodCode);
    }

    [Fact]
    public void CreatePayment_EcoCash_ZiG_Uses_Correct_Code()
    {
        var customer = new Customer("a@b.com", "0771234567", "John");
        var payment = _client.CreatePayment(CurrencyCode.ZiG, PaymentMethodCode.EcoCash, customer);

        Assert.Equal("PZW201", payment.PaymentMethodCode);
    }

    [Theory]
    [InlineData(PaymentMethodCode.Visa, CurrencyCode.USD, "PZW204")]
    [InlineData(PaymentMethodCode.MasterCard, CurrencyCode.USD, "PZW205")]
    [InlineData(PaymentMethodCode.Zimswitch, CurrencyCode.USD, "PZW215")]
    [InlineData(PaymentMethodCode.Omari, CurrencyCode.USD, "PZW216")]
    [InlineData(PaymentMethodCode.PayGo, CurrencyCode.ZiG, "PZW210")]
    public void CreatePayment_Enum_Resolves_Correct_Code(PaymentMethodCode method, CurrencyCode currency, string expectedCode)
    {
        var customer = new Customer("a@b.com", "0771234567", "Test");
        var payment = _client.CreatePayment(currency, method, customer);

        Assert.Equal(expectedCode, payment.PaymentMethodCode);
    }

    [Fact]
    public void CreatePayment_Enum_Throws_When_Unsupported_Combination()
    {
        var customer = new Customer("a@b.com", "0771234567", "Test");
        Assert.Throws<PesePayException>(() => _client.CreatePayment(CurrencyCode.ZWL, PaymentMethodCode.EcoCash, customer));
    }
}
