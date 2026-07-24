using PizzaShop.Application.Abstractions.Geocoding;
using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Application.Common.Abstractions;
using PizzaShop.Application.Common.Exceptions;
using PizzaShop.Application.Common.Messaging;
using PizzaShop.Application.Customers.Dtos;
using PizzaShop.Domain.Customers;
using PizzaShop.Domain.ValueObjects;

namespace PizzaShop.Application.Customers.Commands;

public sealed class AddCustomerAddressCommandHandler : ICommandHandler<AddCustomerAddressCommand, CustomerAddressDto>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IGeocodingService _geocodingService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public AddCustomerAddressCommandHandler(
        ICustomerRepository customerRepository,
        IGeocodingService geocodingService,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _customerRepository = customerRepository;
        _geocodingService = geocodingService;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<CustomerAddressDto> Handle(AddCustomerAddressCommand command, CancellationToken cancellationToken)
    {
        var customerId = _currentUser.CustomerId
            ?? throw new ForbiddenOperationException("Only registered customers have an address book.");

        var customer = await _customerRepository.GetByIdAsync(customerId, cancellationToken)
            ?? throw new NotFoundException(nameof(Customer), customerId);

        var address = new Address(
            command.Address.Street,
            command.Address.BuildingNumber,
            command.Address.City,
            command.Address.PostalCode,
            command.Address.ApartmentNumber,
            command.Address.Notes);

        var coordinate = await _geocodingService.GeocodeAsync(address, cancellationToken)
            ?? throw new NotFoundException("The address could not be located.");

        var entry = customer.AddAddress(command.Label, new DeliveryAddress(address, coordinate), command.IsDefault);

        await _customerRepository.UpdateAsync(customer, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return CustomerAddressMapper.ToDto(entry);
    }
}
