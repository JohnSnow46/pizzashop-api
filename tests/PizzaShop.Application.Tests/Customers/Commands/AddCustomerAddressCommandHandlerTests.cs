using FluentAssertions;
using Moq;
using PizzaShop.Application.Abstractions.Geocoding;
using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Application.Common.Abstractions;
using PizzaShop.Application.Common.Dtos;
using PizzaShop.Application.Common.Exceptions;
using PizzaShop.Application.Customers.Commands;
using PizzaShop.Domain.Customers;
using PizzaShop.Domain.ValueObjects;

namespace PizzaShop.Application.Tests.Customers.Commands;

public class AddCustomerAddressCommandHandlerTests
{
    private readonly Mock<ICustomerRepository> _customerRepository = new();
    private readonly Mock<IGeocodingService> _geocodingService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICurrentUser> _currentUser = new();

    private AddCustomerAddressCommandHandler CreateHandler() =>
        new(_customerRepository.Object, _geocodingService.Object, _unitOfWork.Object, _currentUser.Object);

    private static AddressDto SampleAddressDto() => new("Main St", "1", "Warsaw", "00-001");

    [Fact]
    public async Task Handle_LoggedInCustomer_AddsAddressAndReturnsDto()
    {
        var customerId = Guid.NewGuid();
        _currentUser.Setup(c => c.CustomerId).Returns(customerId);

        var customer = Customer.Create(Guid.NewGuid(), "Jan Kowalski", "jan@example.com", DateTimeOffset.UtcNow);
        _customerRepository
            .Setup(r => r.GetByIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        _geocodingService
            .Setup(g => g.GeocodeAsync(It.IsAny<Address>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GeoCoordinate(52.0, 21.0));

        var handler = CreateHandler();

        var result = await handler.Handle(new AddCustomerAddressCommand("Dom", SampleAddressDto(), true), CancellationToken.None);

        result.Label.Should().Be("Dom");
        result.IsDefault.Should().BeTrue();
        result.Address.City.Should().Be("Warsaw");
        customer.AddressBook.Should().ContainSingle();
        _customerRepository.Verify(r => r.UpdateAsync(customer, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NoCustomerId_ThrowsForbiddenOperationException()
    {
        _currentUser.Setup(c => c.CustomerId).Returns((Guid?)null);

        var handler = CreateHandler();

        var act = () => handler.Handle(new AddCustomerAddressCommand("Dom", SampleAddressDto(), false), CancellationToken.None);

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

        var act = () => handler.Handle(new AddCustomerAddressCommand("Dom", SampleAddressDto(), false), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_AddressCannotBeGeocoded_ThrowsNotFoundException()
    {
        var customerId = Guid.NewGuid();
        _currentUser.Setup(c => c.CustomerId).Returns(customerId);

        var customer = Customer.Create(Guid.NewGuid(), "Jan Kowalski", "jan@example.com", DateTimeOffset.UtcNow);
        _customerRepository
            .Setup(r => r.GetByIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        _geocodingService
            .Setup(g => g.GeocodeAsync(It.IsAny<Address>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GeoCoordinate?)null);

        var handler = CreateHandler();

        var act = () => handler.Handle(new AddCustomerAddressCommand("Dom", SampleAddressDto(), false), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
