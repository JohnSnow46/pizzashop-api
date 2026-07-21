using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PizzaShop.Api.Auth;
using PizzaShop.Application.Common.Messaging;
using PizzaShop.Application.Loyalty.Dtos;
using PizzaShop.Application.Loyalty.Queries;

namespace PizzaShop.Api.Controllers;

/// <summary>
/// Loyalty endpoints (api-layer.md 6.8). Thin: maps request -> Query, calls
/// <see cref="IDispatcher"/>, maps result -> <see cref="IActionResult"/>. Scoping to the
/// caller's own balance happens inside <c>GetLoyaltyBalanceQueryHandler</c> via
/// <c>ICurrentUser.CustomerId</c> — there is no id parameter here to authorize.
/// </summary>
[ApiController]
[Route("api/loyalty")]
public sealed class LoyaltyController : ControllerBase
{
    private readonly IDispatcher _dispatcher;

    public LoyaltyController(IDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    [HttpGet("balance")]
    [Authorize(Roles = AuthRoles.Customer)]
    public async Task<ActionResult<LoyaltyBalanceDto>> GetBalance(CancellationToken cancellationToken)
    {
        var result = await _dispatcher.Send(new GetLoyaltyBalanceQuery(), cancellationToken);
        return Ok(result);
    }
}
