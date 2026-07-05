using PesePay.Domain;

namespace PesePay.Tests;

public class IPesePayClientTests
{
    [Theory]
    [InlineData("InitiateRedirectPaymentAsync")]
    [InlineData("InitiateSeamlessPaymentAsync")]
    [InlineData("CheckPaymentStatusAsync")]
    [InlineData("PollPaymentAsync")]
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
