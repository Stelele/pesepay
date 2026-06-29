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
        var txn = _client.CreateTransaction(100m, CurrencyCode.Usd, "Payment for order", "ORDER-001");

        Assert.Equal(100m, txn.AmountDetails.Value);
        Assert.Equal(CurrencyCode.Usd, txn.AmountDetails.Currency);
        Assert.Equal("Payment for order", txn.ReasonForPayment);
        Assert.Equal("ORDER-001", txn.MerchantReference);
        Assert.Equal(TransactionType.Basic, txn.Type);
    }

    [Fact]
    public void CreatePayment_Returns_Payment_With_Correct_Values()
    {
        var payment = _client.CreatePayment(CurrencyCode.Zwl, "ecocash", "a@b.com", "123", "John");

        Assert.Equal(CurrencyCode.Zwl, payment.CurrencyCode);
        Assert.Equal("ecocash", payment.PaymentMethodCode);
        Assert.Equal("a@b.com", payment.Customer.Email);
        Assert.Equal("123", payment.Customer.PhoneNumber);
        Assert.Equal("John", payment.Customer.Name);
    }

    [Fact]
    public void CreatePayment_With_Email_Only()
    {
        var payment = _client.CreatePayment(CurrencyCode.Usd, "visa", "a@b.com", null, null);

        Assert.Equal("a@b.com", payment.Customer.Email);
        Assert.Null(payment.Customer.PhoneNumber);
    }
}
