using FluentAssertions;
using Moq;
using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Application.Common.Abstractions;
using PizzaShop.Application.Common.Exceptions;
using PizzaShop.Application.Customers.Queries;
using PizzaShop.Domain.Customers;
using PizzaShop.Domain.ValueObjects;

namespace PizzaShop.Application.Tests.Customers.Queries;

public class GetCustomerAddressesQueryHandlerTests
{
    private readonly Mock<ICustomerRepository> _customerRepository = new();
    private readonly Mock<ICurrentUser> _currentUser = new();

    private GetCustomerAddressesQueryHandler CreateHandler() =>
        new(_customerRepository.Object, _currentUser.Object);

    [Fact]
    public async Task Handle_LoggedInCustomer_ReturnsOwnAddressBook()
    {
        var customerId = Guid.NewGuid();
        _currentUser.Setup(c => c.CustomerId).Returns(customerId);

        var customer = Customer.Create(Guid.NewGuid(), "Jan Kowalski", "jan@example.com", DateTimeOffset.UtcNow);
        customer.AddAddress("Dom", new DeliveryAddress(new Address("Main St", "1", "Warsaw", "00-001"), new GeoCoordinate(52.0, 21.0)), true);
        customer.AddAddress("Praca", new DeliveryAddress(new Address("Office St", "2", "Warsaw", "00-002"), new GeoCoordinate(52.1, 21.1)));
        _customerRepository
            .Setup(r => r.GetByIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        var handler = CreateHandler();

        var result = await handler.Handle(new GetCustomerAddressesQuery(), CancellationToken.None);

        result.Should().HaveCount(2);
        result.Should().ContainSingle(a => a.Label == "Dom" && a.IsDefault);
    }

    [Fact]
    public async Task Handle_NoCustomerId_ThrowsForbiddenOperationException()
    {
        _currentUser.Setup(c => c.CustomerId).Returns((Guid?)null);

        var handler = CreateHandler();

        var act = () => handler.Handle(new GetCustomerAddressesQuery(), CancellationToken.None);

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

        var act = () => handler.Handle(new GetCustomerAddressesQuery(), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
