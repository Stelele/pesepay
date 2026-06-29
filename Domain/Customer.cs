namespace PesePay.Domain;

/// <summary>
/// Customer information for a payment. At least an email address or phone number must be provided.
/// </summary>
/// <remarks>
/// Throws <see cref="PesePayException"/> if both <paramref name="email"/> and <paramref name="phoneNumber"/> are null or empty.
/// </remarks>
public record Customer
{
    /// <summary>Customer email address.</summary>
    public string? Email { get; }

    /// <summary>Customer phone number.</summary>
    public string? PhoneNumber { get; }

    /// <summary>Customer name (optional).</summary>
    public string? Name { get; }

    /// <summary>
    /// Creates a new customer. Either email or phone number must be provided.
    /// </summary>
    public Customer(string? email, string? phoneNumber, string? name)
    {
        if (string.IsNullOrEmpty(email) && string.IsNullOrEmpty(phoneNumber))
            throw new PesePayException("Customer details should have an email and/or phone number.");

        Email = email;
        PhoneNumber = phoneNumber;
        Name = name;
    }
}
