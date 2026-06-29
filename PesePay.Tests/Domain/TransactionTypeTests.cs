using System.Text.Json;
using System.Text.Json.Serialization;

namespace PesePay.Domain.Tests;

public class TransactionTypeTests
{
    [Fact]
    public void TransactionType_Serializes_To_basic()
    {
        var options = new JsonSerializerOptions
        {
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower) }
        };

        var json = JsonSerializer.Serialize(TransactionType.Basic, options);

        Assert.Equal("\"basic\"", json);
    }
}
