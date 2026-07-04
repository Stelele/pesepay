using PesePay.Domain;

namespace PesePay.Tests;

public class IPesePayClientTests
{
    [Theory]
    [InlineData("ResultUrl")]
    [InlineData("ReturnUrl")]
    [InlineData("CreateTransaction")]
    [InlineData("CreatePayment")]
    [InlineData("InitiateTransactionAsync")]
    [InlineData("MakeSeamlessPaymentAsync")]
    [InlineData("MakeSeamlessCardPaymentAsync")]
    [InlineData("CheckPaymentStatusAsync")]
    [InlineData("PollTransactionAsync")]
    [InlineData("GetActiveCurrenciesAsync")]
    [InlineData("GetPaymentMethodsAsync")]
    public void Interface_Defines_Member(string memberName)
    {
        var members = typeof(IPesePayClient).GetMembers()
            .Select(m => m.Name)
            .Distinct();

        Assert.Contains(memberName, members);
    }
}
