namespace PizzaShop.Domain.ValueObjects;

/// <summary>
/// Contact information for an order, always present even for guest orders
/// (domain-model.md 2.5, ADR-0005).
/// </summary>
/// <remarks>
/// domain-model.md notes that <c>Email</c> "would be required if payment is online... to
/// be confirmed with the business" — that rule is explicitly undecided, so it is
/// intentionally NOT enforced here. Revisit once confirmed.
/// </remarks>
public sealed class ContactDetails : IEquatable<ContactDetails>
{
    public string FullName { get; }
    public string PhoneNumber { get; }
    public string? Email { get; }

    public ContactDetails(string fullName, string phoneNumber, string? email = null)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("Full name is required.", nameof(fullName));
        if (string.IsNullOrWhiteSpace(phoneNumber))
            throw new ArgumentException("Phone number is required.", nameof(phoneNumber));

        FullName = fullName;
        PhoneNumber = phoneNumber;
        Email = string.IsNullOrWhiteSpace(email) ? null : email;
    }

    public bool Equals(ContactDetails? other) =>
        other is not null
        && FullName == other.FullName
        && PhoneNumber == other.PhoneNumber
        && Email == other.Email;

    public override bool Equals(object? obj) => Equals(obj as ContactDetails);

    public override int GetHashCode() => HashCode.Combine(FullName, PhoneNumber, Email);

    public static bool operator ==(ContactDetails? left, ContactDetails? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(ContactDetails? left, ContactDetails? right) => !(left == right);
}
