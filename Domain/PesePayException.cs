namespace PesePay.Domain;

public class PesePayException : Exception
{
    public PesePayException(string message) : base(message) { }
    public PesePayException(string message, Exception inner) : base(message, inner) { }
}
