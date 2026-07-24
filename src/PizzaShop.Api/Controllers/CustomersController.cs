using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PizzaShop.Api.Auth;
using PizzaShop.Application.Common.Messaging;
using PizzaShop.Application.Customers.Commands;
using PizzaShop.Application.Customers.Dtos;
using PizzaShop.Application.Customers.Queries;

namespace PizzaShop.Api.Controllers;

/// <summary>
/// Customer address-book endpoints. Thin: maps request -> Command/Query, calls
/// <see cref="IDispatcher"/>, maps result -> <see cref="IActionResult"/>. Scoping to the
/// caller's own address book happens inside the handlers via <c>ICurrentUser.CustomerId</c> —
/// there is no customer id parameter here to authorize.
/// </summary>
[ApiController]
[Route("api/customers")]
[Authorize(Roles = AuthRoles.Customer)]
public sealed class CustomersController : ControllerBase
{
    private readonly IDispatcher _dispatcher;

    public CustomersController(IDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    [HttpGet("addresses")]
    public async Task<ActionResult<IReadOnlyList<CustomerAddressDto>>> GetAddresses(CancellationToken cancellationToken)
    {
        var result = await _dispatcher.Send(new GetCustomerAddressesQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpPost("addresses")]
    public async Task<ActionResult<CustomerAddressDto>> AddAddress(AddCustomerAddressCommand command, CancellationToken cancellationToken)
    {
        var result = await _dispatcher.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("addresses/{id:guid}")]
    public async Task<IActionResult> RemoveAddress(Guid id, CancellationToken cancellationToken)
    {
        await _dispatcher.Send(new RemoveCustomerAddressCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpPatch("addresses/{id:guid}/default")]
    public async Task<IActionResult> SetDefaultAddress(Guid id, CancellationToken cancellationToken)
    {
        await _dispatcher.Send(new SetDefaultCustomerAddressCommand(id), cancellationToken);
        return NoContent();
    }
}
