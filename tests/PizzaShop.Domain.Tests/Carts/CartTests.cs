using FluentAssertions;
using PizzaShop.Domain.Carts;

namespace PizzaShop.Domain.Tests.Carts;

public class CartTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Later = Now.AddMinutes(5);

    private static Cart CreateCart() => Cart.Create(Guid.NewGuid(), Now);

    [Fact]
    public void Create_ValidCustomerId_SetsProperties()
    {
        var customerId = Guid.NewGuid();

        var cart = Cart.Create(customerId, Now);

        cart.CustomerId.Should().Be(customerId);
        cart.Items.Should().BeEmpty();
        cart.UpdatedAt.Should().Be(Now);
    }

    [Fact]
    public void Create_EmptyCustomerId_ThrowsArgumentException()
    {
        var act = () => Cart.Create(Guid.Empty, Now);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AddItem_NewSelection_AddsLine()
    {
        var cart = CreateCart();
        var menuItemId = Guid.NewGuid();

        cart.AddItem(menuItemId, null, [], 2, null, Later);

        cart.Items.Should().ContainSingle(i => i.MenuItemId == menuItemId && i.Quantity == 2);
        cart.UpdatedAt.Should().Be(Later);
    }

    [Fact]
    public void AddItem_SameSelectionTwice_MergesIntoOneLineWithSummedQuantity()
    {
        var cart = CreateCart();
        var menuItemId = Guid.NewGuid();
        var extraId = Guid.NewGuid();

        cart.AddItem(menuItemId, null, [extraId], 2, null, Now);
        cart.AddItem(menuItemId, null, [extraId], 3, null, Later);

        cart.Items.Should().ContainSingle();
        cart.Items.Single().Quantity.Should().Be(5);
    }

    [Fact]
    public void AddItem_SameMenuItemDifferentVariant_AddsSeparateLine()
    {
        var cart = CreateCart();
        var menuItemId = Guid.NewGuid();

        cart.AddItem(menuItemId, Guid.NewGuid(), [], 1, null, Now);
        cart.AddItem(menuItemId, Guid.NewGuid(), [], 1, null, Now);

        cart.Items.Should().HaveCount(2);
    }

    [Fact]
    public void AddItem_MergeExceeding100_ThrowsArgumentOutOfRangeException()
    {
        var cart = CreateCart();
        var menuItemId = Guid.NewGuid();
        cart.AddItem(menuItemId, null, [], 60, null, Now);

        var act = () => cart.AddItem(menuItemId, null, [], 50, null, Later);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void AddItem_ExtraIdsInDifferentOrder_StillMerges()
    {
        var cart = CreateCart();
        var menuItemId = Guid.NewGuid();
        var (a, b) = (Guid.NewGuid(), Guid.NewGuid());

        cart.AddItem(menuItemId, null, [a, b], 1, null, Now);
        cart.AddItem(menuItemId, null, [b, a], 1, null, Later);

        cart.Items.Should().ContainSingle(i => i.Quantity == 2);
    }

    [Fact]
    public void UpdateItemQuantity_ExistingItem_ReplacesQuantity()
    {
        var cart = CreateCart();
        cart.AddItem(Guid.NewGuid(), null, [], 1, null, Now);
        var itemId = cart.Items.Single().Id;

        cart.UpdateItemQuantity(itemId, 7, Later);

        cart.Items.Single().Quantity.Should().Be(7);
        cart.Items.Single().Id.Should().Be(itemId);
        cart.UpdatedAt.Should().Be(Later);
    }

    [Fact]
    public void UpdateItemQuantity_UnknownItem_ThrowsArgumentException()
    {
        var cart = CreateCart();

        var act = () => cart.UpdateItemQuantity(Guid.NewGuid(), 1, Later);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void RemoveItem_ExistingItem_RemovesIt()
    {
        var cart = CreateCart();
        cart.AddItem(Guid.NewGuid(), null, [], 1, null, Now);
        var itemId = cart.Items.Single().Id;

        cart.RemoveItem(itemId, Later);

        cart.Items.Should().BeEmpty();
        cart.UpdatedAt.Should().Be(Later);
    }

    [Fact]
    public void RemoveItem_UnknownItem_ThrowsArgumentException()
    {
        var cart = CreateCart();

        var act = () => cart.RemoveItem(Guid.NewGuid(), Later);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Clear_WithItems_RemovesAllAndUpdatesTimestamp()
    {
        var cart = CreateCart();
        cart.AddItem(Guid.NewGuid(), null, [], 1, null, Now);
        cart.AddItem(Guid.NewGuid(), null, [], 1, null, Now);

        cart.Clear(Later);

        cart.Items.Should().BeEmpty();
        cart.UpdatedAt.Should().Be(Later);
    }
}
