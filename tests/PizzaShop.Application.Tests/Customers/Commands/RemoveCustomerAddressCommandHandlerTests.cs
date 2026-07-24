using FluentAssertions;
using Moq;
using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Application.Common.Abstractions;
using PizzaShop.Application.Common.Exceptions;
using PizzaShop.Application.Customers.Commands;
using PizzaShop.Domain.Customers;
using PizzaShop.Domain.ValueObjects;

namespace PizzaShop.Application.Tests.Customers.Commands;

public class RemoveCustomerAddressCommandHandlerTests
{
    private readonly Mock<ICustomerRepository> _customerRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICurrentUser> _currentUser = new();

    private RemoveCustomerAddressCommandHandler CreateHandler() =>
        new(_customerRepository.Object, _unitOfWork.Object, _currentUser.Object);

    [Fact]
    public async Task Handle_ExistingAddress_RemovesItFromAddressBook()
    {
        var customerId = Guid.NewGuid();
        _currentUser.Setup(c => c.CustomerId).Returns(customerId);

        var customer = Customer.Create(Guid.NewGuid(), "Jan Kowalski", "jan@example.com", DateTimeOffset.UtcNow);
        var address = new DeliveryAddress(new Address("Main St", "1", "Warsaw", "00-001"), new GeoCoordinate(52.0, 21.0));
        var entry = customer.AddAddress("Dom", address);
        _customerRepository
            .Setup(r => r.GetByIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        var handler = CreateHandler();

        await handler.Handle(new RemoveCustomerAddressCommand(entry.Id), CancellationToken.None);

        customer.AddressBook.Should().BeEmpty();
        _customerRepository.Verify(r => r.UpdateAsync(customer, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NoCustomerId_ThrowsForbiddenOperationException()
    {
        _currentUser.Setup(c => c.CustomerId).Returns((Guid?)null);

        var handler = CreateHandler();

        var act = () => handler.Handle(new RemoveCustomerAddressCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenOperationException>();
    }

    [Fact]
    public async Task Handle_CustomerNotFound_ThrowsNotFoundException()
    {
        var customerId = Guid.NewGuid();
        _currentUser.Setup(c => c.CustomerId).Returns(customerId);
        _customerRepository
            .Setup(r => r.GetByIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        var handler = CreateHandler();

        var act = () => handler.Handle(new RemoveCustomerAddressCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
