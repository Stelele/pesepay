using System.Text.Json;

namespace PesePay.Domain.Tests;

public class CustomerTests
{
    [Fact]
    public void Customer_Created_With_Email_Only()
    {
        var customer = new Customer("test@example.com", null, null);

        Assert.Equal("test@example.com", customer.Email);
        Assert.Null(customer.PhoneNumber);
        Assert.Null(customer.Name);
    }

    [Fact]
    public void Customer_Serializes_With_SnakeCase()
    {
        var customer = new Customer("a@b.com", "123456", "John");
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        var json = JsonSerializer.Serialize(customer, options);

        Assert.Contains("\"email\":\"a@b.com\"", json);
        Assert.Contains("\"phone_number\":\"123456\"", json);
        Assert.Contains("\"name\":\"John\"", json);
    }

    [Fact]
    public void Customer_Null_Properties_Omitted()
    {
        var customer = new Customer("a@b.com", null, null);
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        var json = JsonSerializer.Serialize(customer, options);

        Assert.DoesNotContain("phone_number", json);
        Assert.DoesNotContain("name", json);
    }

    [Fact]
    public void Customer_Throws_When_Email_And_Phone_Both_Null()
    {
        var ex = Assert.Throws<PesePayException>(() => new Customer(null, null, "John"));
        Assert.Contains("email", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Customer_With_Phone_Only_Is_Valid()
    {
        var customer = new Customer(null, "123456", null);
        Assert.Null(customer.Email);
        Assert.Equal("123456", customer.PhoneNumber);
    }
}
