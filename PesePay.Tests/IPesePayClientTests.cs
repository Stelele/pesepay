using PesePay.Domain;

namespace PesePay.Tests;

public class IPesePayClientTests
{
    [Fact]
    public void Interface_Defines_All_Expected_Members()
    {
        var type = typeof(IPesePayClient);

        Assert.NotNull(type.GetProperty("ResultUrl"));
        Assert.NotNull(type.GetProperty("ReturnUrl"));

        Assert.NotNull(type.GetMethod("CreateTransaction"));
        Assert.NotNull(type.GetMethod("CreatePayment"));
        Assert.NotNull(type.GetMethod("InitiateTransactionAsync"));
        Assert.NotNull(type.GetMethod("MakeSeamlessPaymentAsync"));
        Assert.NotNull(type.GetMethod("CheckPaymentStatusAsync"));
        Assert.NotNull(type.GetMethod("PollTransactionAsync"));
    }
}
