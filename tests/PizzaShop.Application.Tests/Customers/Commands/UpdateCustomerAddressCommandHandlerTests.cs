using FluentAssertions;
using Moq;
using PizzaShop.Application.Abstractions.Geocoding;
using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Application.Common.Abstractions;
using PizzaShop.Application.Common.Dtos;
using PizzaShop.Application.Common.Exceptions;
using PizzaShop.Application.Customers.Commands;
using PizzaShop.Domain.Customers;
using PizzaShop.Domain.Exceptions;
using PizzaShop.Domain.ValueObjects;

namespace PizzaShop.Application.Tests.Customers.Commands;

public class UpdateCustomerAddressCommandHandlerTests
{
    private readonly Mock<ICustomerRepository> _customerRepository = new();
    private readonly Mock<IGeocodingService> _geocodingService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICurrentUser> _currentUser = new();

    private UpdateCustomerAddressCommandHandler CreateHandler() =>
        new(_customerRepository.Object, _geocodingService.Object, _unitOfWork.Object, _currentUser.Object);

    private static AddressDto SampleAddressDto() => new("Oak St", "2", "Warsaw", "00-002");

    private Customer SetUpCustomerWithAddress(out Guid addressId)
    {
        var customerId = Guid.NewGuid();
        _currentUser.Setup(c => c.CustomerId).Returns(customerId);

        var customer = Customer.Create(Guid.NewGuid(), "Jan Kowalski", "jan@example.com", DateTimeOffset.UtcNow);
        var entry = customer.AddAddress("Home", new DeliveryAddress(
            new Address("Main St", "1", "Warsaw", "00-001"), new GeoCoordinate(52.0, 21.0)));
        addressId = entry.Id;

        _customerRepository
            .Setup(r => r.GetByIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        return customer;
    }

    [Fact]
    public async Task Handle_KnownAddressId_UpdatesLabelAndAddressAndReturnsDto()
    {
        var customer = SetUpCustomerWithAddress(out var addressId);
        _geocodingService
            .Setup(g => g.GeocodeAsync(It.IsAny<Address>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GeoCoordinate(52.1, 21.1));

        var handler = CreateHandler();

        var result = await handler.Handle(
            new UpdateCustomerAddressCommand(addressId, "Home (corrected)", SampleAddressDto()), CancellationToken.None);

        result.Label.Should().Be("Home (corrected)");
        result.Address.Street.Should().Be("Oak St");
        customer.AddressBook.Should().ContainSingle(a => a.Label == "Home (corrected)");
        _customerRepository.Verify(r => r.UpdateAsync(customer, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_UnknownAddressId_ThrowsAddressNotInAddressBookException()
    {
        SetUpCustomerWithAddress(out _);
        _geocodingService
            .Setup(g => g.GeocodeAsync(It.IsAny<Address>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GeoCoordinate(52.1, 21.1));

        var handler = CreateHandler();

        var act = () => handler.Handle(
            new UpdateCustomerAddressCommand(Guid.NewGuid(), "Home", SampleAddressDto()), CancellationToken.None);

        await act.Should().ThrowAsync<AddressNotInAddressBookException>();
    }

    [Fact]
    public async Task Handle_NoCustomerId_ThrowsForbiddenOperationException()
    {
        _currentUser.Setup(c => c.CustomerId).Returns((Guid?)null);

        var handler = CreateHandler();

        var act = () => handler.Handle(
            new UpdateCustomerAddressCommand(Guid.NewGuid(), "Home", SampleAddressDto()), CancellationToken.None);

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

        var act = () => handler.Handle(
            new UpdateCustomerAddressCommand(Guid.NewGuid(), "Home", SampleAddressDto()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_AddressCannotBeGeocoded_ThrowsNotFoundException()
    {
        SetUpCustomerWithAddress(out var addressId);
        _geocodingService
            .Setup(g => g.GeocodeAsync(It.IsAny<Address>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GeoCoordinate?)null);

        var handler = CreateHandler();

        var act = () => handler.Handle(
            new UpdateCustomerAddressCommand(addressId, "Home", SampleAddressDto()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
