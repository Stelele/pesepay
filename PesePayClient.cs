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
    private readonly string? _resultUrl;
    private readonly string? _returnUrl;

    public PesePayClient(
        string integrationKey,
        string encryptionKey,
        EnvironmentType environment = EnvironmentType.Sandbox,
        string? resultUrl = null,
        string? returnUrl = null)
    {
        _integrationKey = integrationKey;
        _environment = environment;
        _crypto = new AesCbcPayloadCrypto(encryptionKey);
        _resultUrl = resultUrl;
        _returnUrl = returnUrl;

        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(GetBaseUrl(environment)),
            Timeout = TimeSpan.FromSeconds(30)
        };
        _httpClient.DefaultRequestHeaders.Add("authorization", integrationKey);
    }

    internal PesePayClient(
        IPayloadCrypto crypto,
        HttpClient httpClient,
        EnvironmentType environment = EnvironmentType.Sandbox,
        string? resultUrl = null,
        string? returnUrl = null)
    {
        _crypto = crypto;
        _httpClient = httpClient;
        _httpClient.BaseAddress ??= new Uri(GetBaseUrl(environment));
        _environment = environment;
        _integrationKey = string.Empty;
        _resultUrl = resultUrl;
        _returnUrl = returnUrl;
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

    internal static string GetPaymentMethodCode(PaymentMethodCode method, CurrencyCode currency)
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

    public async Task<PesepayResult<InitiateResponse>> InitiateRedirectPaymentAsync(
        RedirectPaymentRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_resultUrl))
            throw new PesePayException("Result URL has not been specified.");

        if (string.IsNullOrEmpty(_returnUrl))
            throw new PesePayException("Return URL has not been specified.");

        var transaction = new Transaction(
            new Amount(request.Amount, request.Currency),
            request.Reason,
            request.MerchantReference)
        {
            ResultUrl = _resultUrl,
            ReturnUrl = _returnUrl
        };

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

    public async Task<PesepayResult<PaymentResponse>> InitiateSeamlessPaymentAsync(
        SeamlessPaymentRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_resultUrl))
            throw new PesePayException("Result URL has not been specified.");

        var methodCode = GetPaymentMethodCode(request.Method, request.Currency);
        var customer = new Customer(request.Email, request.PhoneNumber, request.CustomerName);
        var payment = new Payment(request.Currency, methodCode, customer)
        {
            ResultUrl = _resultUrl,
            ReturnUrl = _returnUrl,
            ReasonForPayment = request.Reason,
            AmountDetails = new Amount(request.Amount, request.Currency),
            MerchantReference = request.MerchantReference
        };

        var fields = new Dictionary<string, string>();

        if (request.Card != null)
        {
            fields["creditCardNumber"] = request.Card.Number;
            fields["creditCardSecurityNumber"] = request.Card.Cvv;
            fields["creditCardExpiryDate"] = request.Card.ExpiryDate;
            if (!string.IsNullOrEmpty(request.Card.HolderName))
                fields["creditCardHolder"] = request.Card.HolderName;
        }
        else if (!string.IsNullOrEmpty(request.PhoneNumber))
        {
            fields["customerPhoneNumber"] = request.PhoneNumber;
        }
        else
        {
            throw new PesePayException(
                "Seamless payment requires either a Card (for card payments) or PhoneNumber (for mobile money).");
        }

        payment.PaymentMethodRequiredFields = fields;
        payment.PaymentRequestFields = fields;

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

    public async Task<PesepayResult<PaymentStatus>> CheckPaymentStatusAsync(
        string referenceNumber, CancellationToken ct = default)
    {
        var url = $"v1/payments/check-payment?referenceNumber={Uri.EscapeDataString(referenceNumber)}";
        return await PollPaymentAsync(new Uri(url, UriKind.Relative), ct);
    }

    public async Task<PesepayResult<PaymentStatus>> PollPaymentAsync(
        Uri pollUrl, CancellationToken ct = default)
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

    public async Task<PesepayResult<List<CurrencyInfo>>> GetActiveCurrenciesAsync(
        CancellationToken ct = default)
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

    public async Task<PesepayResult<List<PaymentMethodInfo>>> GetPaymentMethodsAsync(
        string currencyCode, CancellationToken ct = default)
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
