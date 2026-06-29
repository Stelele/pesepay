namespace PesePay.Domain.Tests;

public class PesepayResultTests
{
    [Fact]
    public void Ok_Creates_Success_Result()
    {
        var result = PesepayResult<string>.Ok("hello");

        Assert.True(result.IsSuccess);
        Assert.Equal("hello", result.Data);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void Fail_Creates_Failure_Result()
    {
        var result = PesepayResult<string>.Fail("Something went wrong");

        Assert.False(result.IsSuccess);
        Assert.Null(result.Data);
        Assert.Equal("Something went wrong", result.ErrorMessage);
    }

    [Fact]
    public void Ok_With_ReferenceType()
    {
        var data = new { Name = "test" };
        var result = PesepayResult<object>.Ok(data);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
    }
}
