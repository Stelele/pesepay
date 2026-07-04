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
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly string _integrationKey;
    private readonly EnvironmentType _environment;
    private readonly IPayloadCrypto _crypto;
    private readonly HttpClient _httpClient;

    public string? ResultUrl { get; set; }
    public string? ReturnUrl { get; set; }

    public PesePayClient(string integrationKey, string encryptionKey, EnvironmentType environment = EnvironmentType.Sandbox)
    {
        _integrationKey = integrationKey;
        _environment = environment;
        _crypto = new AesCbcPayloadCrypto(encryptionKey);

        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(GetBaseUrl(environment)),
            Timeout = TimeSpan.FromSeconds(30)
        };
        _httpClient.DefaultRequestHeaders.Add("authorization", integrationKey);
    }

    internal PesePayClient(IPayloadCrypto crypto, HttpClient httpClient, EnvironmentType environment = EnvironmentType.Sandbox)
    {
        _crypto = crypto;
        _httpClient = httpClient;
        _httpClient.BaseAddress ??= new Uri(GetBaseUrl(environment));
        _environment = environment;
        _integrationKey = string.Empty;
    }

    private static string GetBaseUrl(EnvironmentType environment) => environment switch
    {
        EnvironmentType.Production => "https://api.pesepay.com/api/payments-engine/",
        EnvironmentType.Sandbox => "https://api.test.sandbox.pesepay.com/payments-engine/",
        _ => throw new ArgumentOutOfRangeException(nameof(environment))
    };

    private string MakePaymentPath => "v2/payments/make-payment";

    private static readonly Dictionary<(PaymentMethodCode, CurrencyCode), string> _methodCodes = new()
    {
        [(PaymentMethodCode.EcoCash, CurrencyCode.USD)] = "PZW211",
        [(PaymentMethodCode.EcoCash, CurrencyCode.ZiG)] = "PZW201",
        [(PaymentMethodCode.InnBucks, CurrencyCode.USD)] = "PZW212",
        [(PaymentMethodCode.Visa, CurrencyCode.USD)] = "PZW204",
        [(PaymentMethodCode.MasterCard, CurrencyCode.USD)] = "PZW205",
        [(PaymentMethodCode.Zimswitch, CurrencyCode.USD)] = "PZW215",
        [(PaymentMethodCode.Omari, CurrencyCode.USD)] = "PZW216",
        [(PaymentMethodCode.PayGo, CurrencyCode.ZiG)] = "PZW210",
    };

    private static string GetPaymentMethodCode(PaymentMethodCode method, CurrencyCode currency)
    {
        if (_methodCodes.TryGetValue((method, currency), out var code))
            return code;

        throw new PesePayException($"Payment method {method} is not available for {currency}.");
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException(
                $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Body: {body}");
        }
    }

    private string DecryptResponsePayload(string responseBody)
    {
        var raw = JsonSerializer.Deserialize<JsonElement>(responseBody);
        if (raw.TryGetProperty("payload", out var payloadElement))
        {
            try
            {
                return _crypto.Decrypt(payloadElement.GetString()!);
            }
            catch (System.Security.Cryptography.CryptographicException)
            {
                return responseBody;
            }
        }

        return responseBody;
    }

    public Transaction CreateTransaction(decimal amount, CurrencyCode currency, string reason, string? merchantRef = null)
    {
        return new Transaction(
            new Amount(amount, currency),
            reason,
            merchantRef);
    }

    public Payment CreatePayment(CurrencyCode currency, PaymentMethodCode method, Customer customer)
    {
        return new Payment(currency, GetPaymentMethodCode(method, currency), customer);
    }

    public Payment CreatePayment(CurrencyCode currency, string methodCode, string? email, string? phone, string? name)
    {
        var customer = new Customer(email, phone, name);
        return new Payment(currency, methodCode, customer);
    }

    public async Task<PesepayResult<InitiateResponse>> InitiateTransactionAsync(decimal amount, CurrencyCode currency, string reason, string? merchantReference, string resultUrl, string returnUrl, CancellationToken ct = default)
    {
        ResultUrl = resultUrl;
        ReturnUrl = returnUrl;

        var transaction = CreateTransaction(amount, currency, reason, merchantReference);
        return await InitiateTransactionAsync(transaction, ct);
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

            var response = await _httpClient.PostAsync("v1/payments/initiate", content, ct);
            await EnsureSuccessAsync(response);

            var responseBody = await response.Content.ReadAsStringAsync(ct);
            var json = DecryptResponsePayload(responseBody);
            var result = JsonSerializer.Deserialize<InitiateResponse>(json, _jsonOptions)!;

            return PesepayResult<InitiateResponse>.Ok(result);
        }
        catch (HttpRequestException ex)
        {
            throw new PesePayException(ex.Message, ex);
        }
    }

    public async Task<PesepayResult<PaymentResponse>> MakeSeamlessPaymentAsync(PaymentMethodCode method, CurrencyCode currency, decimal amount, string phoneNumber, string email, string? customerName, string reason, string merchantReference, CancellationToken ct = default)
    {
        var customer = new Customer(email, phoneNumber, customerName);
        var payment = CreatePayment(currency, method, customer);

        var fields = new Dictionary<string, string>
        {
            { "customerPhoneNumber", phoneNumber }
        };

        return await MakeSeamlessPaymentAsync(payment, reason, amount, merchantReference, fields, ct);
    }

    public async Task<PesepayResult<PaymentResponse>> MakeSeamlessCardPaymentAsync(PaymentMethodCode method, CurrencyCode currency, decimal amount, CardDetails card, string? email, string? customerName, string reason, string merchantReference, CancellationToken ct = default)
    {
        var customer = new Customer(email, null, customerName);
        var payment = CreatePayment(currency, method, customer);

        var fields = new Dictionary<string, string>
        {
            { "creditCardNumber", card.Number },
            { "creditCardSecurityNumber", card.Cvv },
            { "creditCardExpiryDate", card.ExpiryDate }
        };

        if (!string.IsNullOrEmpty(card.HolderName))
            fields["creditCardHolder"] = card.HolderName;

        return await MakeSeamlessPaymentAsync(payment, reason, amount, merchantReference, fields, ct);
    }

    public async Task<PesepayResult<PaymentResponse>> MakeSeamlessPaymentAsync(Payment payment, string reason, decimal amount, string merchantReference, Dictionary<string, string>? fields = null, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(ResultUrl))
            throw new PesePayException("Result URL has not been specified.");

        payment.ResultUrl = ResultUrl;
        payment.ReturnUrl = ReturnUrl;
        payment.ReasonForPayment = reason;
        payment.AmountDetails = new Amount(amount, payment.CurrencyCode);
        payment.MerchantReference = merchantReference;
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

            var response = await _httpClient.PostAsync(MakePaymentPath, content, ct);
            await EnsureSuccessAsync(response);

            var responseBody = await response.Content.ReadAsStringAsync(ct);
            var json = DecryptResponsePayload(responseBody);
            var result = JsonSerializer.Deserialize<PaymentResponse>(json, _jsonOptions)!;

            return PesepayResult<PaymentResponse>.Ok(result);
        }
        catch (HttpRequestException ex)
        {
            throw new PesePayException(ex.Message, ex);
        }
    }

    public async Task<PesepayResult<PaymentStatus>> CheckPaymentStatusAsync(string referenceNumber, CancellationToken ct = default)
    {
        var url = $"v1/payments/check-payment?referenceNumber={Uri.EscapeDataString(referenceNumber)}";
        return await PollTransactionAsync(new Uri(url, UriKind.Relative), ct);
    }

    public async Task<PesepayResult<PaymentStatus>> PollTransactionAsync(Uri pollUrl, CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(pollUrl, ct);
            await EnsureSuccessAsync(response);

            var responseBody = await response.Content.ReadAsStringAsync(ct);
            var json = DecryptResponsePayload(responseBody);
            var result = JsonSerializer.Deserialize<PaymentStatus>(json, _jsonOptions)!;

            return PesepayResult<PaymentStatus>.Ok(result);
        }
        catch (HttpRequestException ex)
        {
            throw new PesePayException(ex.Message, ex);
        }
    }

    public async Task<PesepayResult<List<CurrencyInfo>>> GetActiveCurrenciesAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("v1/currencies/active", ct);
            await EnsureSuccessAsync(response);

            var responseBody = await response.Content.ReadAsStringAsync(ct);
            var result = JsonSerializer.Deserialize<List<CurrencyInfo>>(responseBody, _jsonOptions)!;

            return PesepayResult<List<CurrencyInfo>>.Ok(result);
        }
        catch (HttpRequestException ex)
        {
            throw new PesePayException(ex.Message, ex);
        }
    }

    public async Task<PesepayResult<List<PaymentMethodInfo>>> GetPaymentMethodsAsync(string currencyCode, CancellationToken ct = default)
    {
        try
        {
            var url = $"v1/payment-methods/for-currency?currencyCode={Uri.EscapeDataString(currencyCode)}";
            var response = await _httpClient.GetAsync(url, ct);
            await EnsureSuccessAsync(response);

            var responseBody = await response.Content.ReadAsStringAsync(ct);
            var result = JsonSerializer.Deserialize<List<PaymentMethodInfo>>(responseBody, _jsonOptions)!;

            return PesepayResult<List<PaymentMethodInfo>>.Ok(result);
        }
        catch (HttpRequestException ex)
        {
            throw new PesePayException(ex.Message, ex);
        }
    }
}
