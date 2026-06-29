namespace PesePay.Crypto;

public interface IPayloadCrypto
{
    string Encrypt(string jsonPayload);
    string Decrypt(string base64Payload);
}
