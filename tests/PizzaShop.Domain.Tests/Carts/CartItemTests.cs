using FluentAssertions;
using PizzaShop.Domain.Carts;

namespace PizzaShop.Domain.Tests.Carts;

public class CartItemTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void AddItem_EmptyMenuItemId_ThrowsArgumentException()
    {
        var cart = Cart.Create(Guid.NewGuid(), Now);

        var act = () => cart.AddItem(Guid.Empty, null, [], 1, null, Now);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    [InlineData(-1)]
    public void AddItem_QuantityOutOfRange_ThrowsArgumentOutOfRangeException(int quantity)
    {
        var cart = Cart.Create(Guid.NewGuid(), Now);

        var act = () => cart.AddItem(Guid.NewGuid(), null, [], quantity, null, Now);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void AddItem_NotesExceedingMaxLength_ThrowsArgumentException()
    {
        var cart = Cart.Create(Guid.NewGuid(), Now);
        var tooLong = new string('a', 501);

        var act = () => cart.AddItem(Guid.NewGuid(), null, [], 1, tooLong, Now);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AddItem_WhitespaceOnlyNotes_StoresNull()
    {
        var cart = Cart.Create(Guid.NewGuid(), Now);

        cart.AddItem(Guid.NewGuid(), null, [], 1, "   ", Now);

        cart.Items.Single().Notes.Should().BeNull();
    }

    [Fact]
    public void AddItem_DuplicateExtraIds_Deduplicated()
    {
        var cart = Cart.Create(Guid.NewGuid(), Now);
        var extraId = Guid.NewGuid();

        cart.AddItem(Guid.NewGuid(), null, [extraId, extraId], 1, null, Now);

        cart.Items.Single().ExtraIds.Should().ContainSingle().Which.Should().Be(extraId);
    }
}
