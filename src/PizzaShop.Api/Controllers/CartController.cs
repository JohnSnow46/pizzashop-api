using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PizzaShop.Api.Auth;
using PizzaShop.Application.Carts.Commands;
using PizzaShop.Application.Carts.Dtos;
using PizzaShop.Application.Carts.Queries;
using PizzaShop.Application.Common.Messaging;

namespace PizzaShop.Api.Controllers;

/// <summary>
/// Cart endpoints (ADR-0043). Thin: maps request -> Command/Query, calls
/// <see cref="IDispatcher"/>, maps result -> <see cref="IActionResult"/>. No business logic.
/// Registered customers only — guests keep using the existing client-side cart.
/// </summary>
[ApiController]
[Authorize(Roles = AuthRoles.Customer)]
[Route("api/cart")]
public sealed class CartController : ControllerBase
{
    private readonly IDispatcher _dispatcher;

    public CartController(IDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    [HttpGet]
    public async Task<ActionResult<CartDto>> Get(CancellationToken cancellationToken)
    {
        var result = await _dispatcher.Send(new GetMyCartQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpPost("items")]
    public async Task<ActionResult<Guid>> AddItem(AddCartItemCommand command, CancellationToken cancellationToken)
    {
        var id = await _dispatcher.Send(command, cancellationToken);
        return Ok(id);
    }

    [HttpPatch("items/{itemId:guid}")]
    public async Task<IActionResult> UpdateItemQuantity(
        Guid itemId, [FromBody] UpdateCartItemQuantityBody body, CancellationToken cancellationToken)
    {
        await _dispatcher.Send(new UpdateCartItemQuantityCommand(itemId, body.Quantity), cancellationToken);
        return NoContent();
    }

    [HttpDelete("items/{itemId:guid}")]
    public async Task<IActionResult> RemoveItem(Guid itemId, CancellationToken cancellationToken)
    {
        await _dispatcher.Send(new RemoveCartItemCommand(itemId), cancellationToken);
        return NoContent();
    }

    [HttpDelete]
    public async Task<IActionResult> Clear(CancellationToken cancellationToken)
    {
        await _dispatcher.Send(new ClearCartCommand(), cancellationToken);
        return NoContent();
    }

    public sealed record UpdateCartItemQuantityBody(int Quantity);
}
