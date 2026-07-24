using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Application.Common.Abstractions;
using PizzaShop.Application.Common.Exceptions;
using PizzaShop.Application.Common.Messaging;
using PizzaShop.Application.Customers.Dtos;
using PizzaShop.Domain.Customers;

namespace PizzaShop.Application.Customers.Queries;

public sealed class GetCustomerAddressesQueryHandler : IQueryHandler<GetCustomerAddressesQuery, IReadOnlyList<CustomerAddressDto>>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly ICurrentUser _currentUser;

    public GetCustomerAddressesQueryHandler(ICustomerRepository customerRepository, ICurrentUser currentUser)
    {
        _customerRepository = customerRepository;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<CustomerAddressDto>> Handle(GetCustomerAddressesQuery query, CancellationToken cancellationToken)
    {
        var customerId = _currentUser.CustomerId
            ?? throw new ForbiddenOperationException("Only registered customers have an address book.");

        var customer = await _customerRepository.GetByIdAsync(customerId, cancellationToken)
            ?? throw new NotFoundException(nameof(Customer), customerId);

        return customer.AddressBook.Select(CustomerAddressMapper.ToDto).ToList();
    }
}
