using PizzaShop.Domain.ValueObjects;

namespace PizzaShop.Domain.Customers;

/// <summary>
/// Address-book entry: a child entity wrapping a <see cref="ValueObjects.DeliveryAddress"/>
/// with an identity and a user-facing label (domain-model.md 2.4, 6).
/// </summary>
public class CustomerAddress
{
    public Guid Id { get; }
    public string Label { get; private set; }
    public DeliveryAddress DeliveryAddress { get; private set; }
    public bool IsDefault { get; private set; }

    // EF Core materialization only (ADR-0020) — not used by Domain logic.
    private CustomerAddress()
    {
    }

    private CustomerAddress(Guid id, string label, DeliveryAddress deliveryAddress, bool isDefault)
    {
        Id = id;
        Label = label;
        DeliveryAddress = deliveryAddress;
        IsDefault = isDefault;
    }

    internal static CustomerAddress Create(string label, DeliveryAddress deliveryAddress, bool isDefault)
    {
        if (string.IsNullOrWhiteSpace(label))
            throw new ArgumentException("Label is required.", nameof(label));
        ArgumentNullException.ThrowIfNull(deliveryAddress);

        return new CustomerAddress(Guid.NewGuid(), label, deliveryAddress, isDefault);
    }

    internal void SetAsDefault() => IsDefault = true;

    internal void UnsetDefault() => IsDefault = false;

    public void Update(string label, DeliveryAddress deliveryAddress)
    {
        if (string.IsNullOrWhiteSpace(label))
            throw new ArgumentException("Label is required.", nameof(label));
        ArgumentNullException.ThrowIfNull(deliveryAddress);

        Label = label;
        DeliveryAddress = deliveryAddress;
    }
}
