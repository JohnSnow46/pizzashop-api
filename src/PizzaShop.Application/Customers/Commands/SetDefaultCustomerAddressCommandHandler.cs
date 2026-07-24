using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Application.Common.Abstractions;
using PizzaShop.Application.Common.Exceptions;
using PizzaShop.Application.Common.Messaging;
using PizzaShop.Domain.Customers;

namespace PizzaShop.Application.Customers.Commands;

public sealed class SetDefaultCustomerAddressCommandHandler : ICommandHandler<SetDefaultCustomerAddressCommand>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public SetDefaultCustomerAddressCommandHandler(
        ICustomerRepository customerRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _customerRepository = customerRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(SetDefaultCustomerAddressCommand command, CancellationToken cancellationToken)
    {
        var customerId = _currentUser.CustomerId
            ?? throw new ForbiddenOperationException("Only registered customers have an address book.");

        var customer = await _customerRepository.GetByIdAsync(customerId, cancellationToken)
            ?? throw new NotFoundException(nameof(Customer), customerId);

        // Customer.SetDefaultAddress throws AddressNotInAddressBookException (Domain) if
        // AddressId isn't part of this customer's book — ExceptionHandler maps it to 422.
        customer.SetDefaultAddress(command.AddressId);

        await _customerRepository.UpdateAsync(customer, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
