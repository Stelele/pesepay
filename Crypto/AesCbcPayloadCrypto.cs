using System.Security.Cryptography;
using System.Text;

namespace PesePay.Crypto;

public class AesCbcPayloadCrypto : IPayloadCrypto
{
    private readonly byte[] _key;
    private readonly byte[] _iv;

    public AesCbcPayloadCrypto(string encryptionKey)
    {
        _key = Encoding.UTF8.GetBytes(encryptionKey);
        _iv = Encoding.UTF8.GetBytes(encryptionKey[..Math.Min(16, encryptionKey.Length)]);
    }

    public string Encrypt(string jsonPayload)
    {
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = _iv;
        aes.Mode = CipherMode.CBC;

        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(jsonPayload);
        var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
        return Convert.ToBase64String(cipherBytes);
    }

    public string Decrypt(string base64Payload)
    {
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = _iv;
        aes.Mode = CipherMode.CBC;

        using var decryptor = aes.CreateDecryptor();
        var cipherBytes = Convert.FromBase64String(base64Payload);
        var plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
        return Encoding.UTF8.GetString(plainBytes);
    }
}
