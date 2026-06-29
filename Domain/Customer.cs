namespace PesePay.Domain;

public record Customer
{
    public string? Email { get; }
    public string? PhoneNumber { get; }
    public string? Name { get; }

    public Customer(string? email, string? phoneNumber, string? name)
    {
        if (string.IsNullOrEmpty(email) && string.IsNullOrEmpty(phoneNumber))
            throw new PesePayException("Customer details should have an email and/or phone number.");

        Email = email;
        PhoneNumber = phoneNumber;
        Name = name;
    }
}
