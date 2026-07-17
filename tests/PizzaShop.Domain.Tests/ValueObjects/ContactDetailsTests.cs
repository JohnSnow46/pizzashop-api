using FluentAssertions;
using PizzaShop.Domain.ValueObjects;

namespace PizzaShop.Domain.Tests.ValueObjects;

public class ContactDetailsTests
{
    [Fact]
    public void Constructor_FullNameMissing_ThrowsArgumentException()
    {
        var act = () => new ContactDetails("", "123456789");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_PhoneNumberMissing_ThrowsArgumentException()
    {
        var act = () => new ContactDetails("Jan Kowalski", "");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_EmailNotProvided_IsNull()
    {
        var contact = new ContactDetails("Jan Kowalski", "123456789");

        contact.Email.Should().BeNull();
    }

    [Fact]
    public void Constructor_BlankEmail_IsNormalizedToNull()
    {
        var contact = new ContactDetails("Jan Kowalski", "123456789", "   ");

        contact.Email.Should().BeNull();
    }

    [Fact]
    public void Equals_SameValues_ReturnsTrue()
    {
        var a = new ContactDetails("Jan Kowalski", "123456789", "jan@example.com");
        var b = new ContactDetails("Jan Kowalski", "123456789", "jan@example.com");

        (a == b).Should().BeTrue();
    }
}
