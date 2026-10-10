using PizzaShop.Application.Abstractions.Geocoding;
using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Application.Common.Abstractions;
using PizzaShop.Application.Common.Exceptions;
using PizzaShop.Application.Common.Messaging;
using PizzaShop.Application.Customers.Dtos;
using PizzaShop.Domain.Customers;
using PizzaShop.Domain.ValueObjects;

namespace PizzaShop.Application.Customers.Commands;

public sealed class UpdateCustomerAddressCommandHandler : ICommandHandler<UpdateCustomerAddressCommand, CustomerAddressDto>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IGeocodingService _geocodingService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public UpdateCustomerAddressCommandHandler(
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

    public async Task<CustomerAddressDto> Handle(UpdateCustomerAddressCommand command, CancellationToken cancellationToken)
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

        // Re-geocoded like AddCustomerAddressCommandHandler -- the label/street/city can all
        // change here, so the stored coordinate must be recomputed rather than carried over.
        var coordinate = await _geocodingService.GeocodeAsync(address, cancellationToken)
            ?? throw new NotFoundException("The address could not be located.");

        // Customer.UpdateAddress throws AddressNotInAddressBookException (Domain) if
        // AddressId isn't part of this customer's book -- ExceptionHandler maps it to 422.
        customer.UpdateAddress(command.AddressId, command.Label, new DeliveryAddress(address, coordinate));

        await _customerRepository.UpdateAsync(customer, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var updated = customer.AddressBook.First(a => a.Id == command.AddressId);
        return CustomerAddressMapper.ToDto(updated);
    }
}
