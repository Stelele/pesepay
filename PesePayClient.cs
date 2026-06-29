using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using PesePay.Crypto;
using PesePay.Domain;

namespace PesePay;

public class PesePayClient : IPesePayClient
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower) },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly IPayloadCrypto _crypto;
    private readonly HttpClient _httpClient;

    public string? ResultUrl { get; set; }
    public string? ReturnUrl { get; set; }

    public PesePayClient(string integrationKey, string encryptionKey, EnvironmentType environment = EnvironmentType.Sandbox)
    {
        _crypto = new AesCbcPayloadCrypto(encryptionKey);

        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(GetBaseUrl(environment)),
            Timeout = TimeSpan.FromSeconds(30)
        };
        _httpClient.DefaultRequestHeaders.Add("key", integrationKey);
    }

    internal PesePayClient(IPayloadCrypto crypto, HttpClient httpClient, EnvironmentType environment = EnvironmentType.Sandbox)
    {
        _crypto = crypto;
        _httpClient = httpClient;
    }

    private static string GetBaseUrl(EnvironmentType environment) => environment switch
    {
        EnvironmentType.Production => "https://api.pesepay.com/api/payments-engine",
        EnvironmentType.Sandbox => "https://api.test.sandbox.pesepay.com/payments-engine",
        _ => throw new ArgumentOutOfRangeException(nameof(environment))
    };

    public Transaction CreateTransaction(decimal amount, CurrencyCode currency, string reason, string? merchantRef = null)
    {
        return new Transaction(
            new Amount(amount, currency),
            reason,
            merchantRef);
    }

    public Payment CreatePayment(CurrencyCode currency, string methodCode, string? email, string? phone, string? name)
    {
        var customer = new Customer(email, phone, name);
        return new Payment(currency, methodCode, customer);
    }

    public async Task<PesepayResult<InitiateResponse>> InitiateTransactionAsync(Transaction transaction, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(ResultUrl))
            throw new PesePayException("Result URL has not been specified.");

        if (string.IsNullOrEmpty(ReturnUrl))
            throw new PesePayException("Return URL has not been specified.");

        transaction.ResultUrl = ResultUrl;
        transaction.ReturnUrl = ReturnUrl;

        var payload = _crypto.Encrypt(JsonSerializer.Serialize(transaction, _jsonOptions));

        try
        {
            var content = new StringContent(
                JsonSerializer.Serialize(new { payload }, _jsonOptions),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync("/v1/payments/initiate", content, ct);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync(ct);
            var raw = JsonSerializer.Deserialize<JsonElement>(responseBody);
            var decrypted = _crypto.Decrypt(raw.GetProperty("payload").GetString()!);
            var result = JsonSerializer.Deserialize<InitiateResponse>(decrypted, _jsonOptions)!;

            return PesepayResult<InitiateResponse>.Ok(result);
        }
        catch (HttpRequestException ex)
        {
            throw new PesePayException(ex.Message, ex);
        }
    }

    public async Task<PesepayResult<PaymentResponse>> MakeSeamlessPaymentAsync(Payment payment, string reason, decimal amount, Dictionary<string, string>? fields = null, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(ResultUrl))
            throw new PesePayException("Result URL has not been specified.");

        payment.ResultUrl = ResultUrl;
        payment.ReturnUrl = ReturnUrl;
        payment.ReasonForPayment = reason;
        payment.AmountDetails = new Amount(amount, payment.CurrencyCode);
        if (fields != null)
        {
            payment.PaymentMethodRequiredFields = fields;
            payment.PaymentRequestFields = fields;
        }

        var payload = _crypto.Encrypt(JsonSerializer.Serialize(payment, _jsonOptions));

        try
        {
            var content = new StringContent(
                JsonSerializer.Serialize(new { payload }, _jsonOptions),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync("/v2/payments/make-payment", content, ct);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync(ct);
            var raw = JsonSerializer.Deserialize<JsonElement>(responseBody);
            var decrypted = _crypto.Decrypt(raw.GetProperty("payload").GetString()!);
            var result = JsonSerializer.Deserialize<PaymentResponse>(decrypted, _jsonOptions)!;

            return PesepayResult<PaymentResponse>.Ok(result);
        }
        catch (HttpRequestException ex)
        {
            throw new PesePayException(ex.Message, ex);
        }
    }

    public async Task<PesepayResult<PaymentStatus>> CheckPaymentStatusAsync(string referenceNumber, CancellationToken ct = default)
    {
        var url = $"/v1/payments/check-payment?referenceNumber={Uri.EscapeDataString(referenceNumber)}";
        return await PollTransactionAsync(new Uri(url, UriKind.Relative), ct);
    }

    public async Task<PesepayResult<PaymentStatus>> PollTransactionAsync(Uri pollUrl, CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(pollUrl, ct);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync(ct);
            var raw = JsonSerializer.Deserialize<JsonElement>(responseBody);
            var decrypted = _crypto.Decrypt(raw.GetProperty("payload").GetString()!);
            var result = JsonSerializer.Deserialize<PaymentStatus>(decrypted, _jsonOptions)!;

            return PesepayResult<PaymentStatus>.Ok(result);
        }
        catch (HttpRequestException ex)
        {
            throw new PesePayException(ex.Message, ex);
        }
    }
}
