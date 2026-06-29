namespace PesePay.Domain;

public class PesepayResult<T>
    where T : notnull
{
    public bool IsSuccess { get; }
    public T? Data { get; }
    public string? ErrorMessage { get; }

    private PesepayResult(bool isSuccess, T? data, string? errorMessage)
    {
        IsSuccess = isSuccess;
        Data = data;
        ErrorMessage = errorMessage;
    }

    public static PesepayResult<T> Ok(T data) =>
        new(true, data, null);

    public static PesepayResult<T> Fail(string errorMessage) =>
        new(false, default, errorMessage);
}
