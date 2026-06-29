using System.Text;

namespace PesePay.Crypto.Tests;

public class AesCbcPayloadCryptoTests
{
    private const string EncryptionKey = "0123456789abcdef0123456789abcdef";
    private const string Payload = """{"amount":100,"currency":"USD"}""";

    [Fact]
    public void Encrypt_Produces_Base64_String()
    {
        var crypto = new AesCbcPayloadCrypto(EncryptionKey);

        var encrypted = crypto.Encrypt(Payload);

        Assert.NotNull(encrypted);
        Assert.NotEmpty(encrypted);
        Assert.NotEqual(Payload, encrypted);
    }

    [Fact]
    public void Decrypt_Roundtrip_Returns_Original()
    {
        var crypto = new AesCbcPayloadCrypto(EncryptionKey);

        var encrypted = crypto.Encrypt(Payload);
        var decrypted = crypto.Decrypt(encrypted);

        Assert.Equal(Payload, decrypted);
    }

    [Fact]
    public void Encrypt_Deterministic_Same_Input_Same_Output()
    {
        var crypto = new AesCbcPayloadCrypto(EncryptionKey);

        var enc1 = crypto.Encrypt(Payload);
        var enc2 = crypto.Encrypt(Payload);

        Assert.Equal(enc1, enc2);
    }

    [Fact]
    public void Decrypt_With_Wrong_Key_Throws()
    {
        var crypto1 = new AesCbcPayloadCrypto("abcdefghijklmnopqrstuvwxyz123456");
        var crypto2 = new AesCbcPayloadCrypto("zyxwvutsrqponmlkjihgfedcba654321");

        var encrypted = crypto1.Encrypt(Payload);

        Assert.ThrowsAny<Exception>(() => crypto2.Decrypt(encrypted));
    }
}
