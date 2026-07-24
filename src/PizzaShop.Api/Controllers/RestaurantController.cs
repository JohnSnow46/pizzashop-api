using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PizzaShop.Api.Auth;
using PizzaShop.Application.Common.Messaging;
using PizzaShop.Application.Restaurant.Commands;
using PizzaShop.Application.Restaurant.Dtos;
using PizzaShop.Application.Restaurant.Queries;

namespace PizzaShop.Api.Controllers;

/// <summary>
/// Single-tenant restaurant configuration endpoints (api-layer.md 6.4). Thin: maps request
/// -> Command/Query, calls <see cref="IDispatcher"/>, maps result -> <see cref="IActionResult"/>.
/// No business logic.
/// </summary>
[ApiController]
[Route("api/restaurant")]
public sealed class RestaurantController : ControllerBase
{
    private readonly IDispatcher _dispatcher;

    public RestaurantController(IDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    [HttpGet("config")]
    [AllowAnonymous]
    public async Task<ActionResult<RestaurantConfigDto>> GetConfig(CancellationToken cancellationToken)
    {
        var result = await _dispatcher.Send(new GetRestaurantConfigQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("info")]
    [AllowAnonymous]
    public async Task<ActionResult<RestaurantInfoDto>> GetInfo(CancellationToken cancellationToken)
    {
        var result = await _dispatcher.Send(new GetRestaurantInfoQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpPut("opening-hours")]
    [Authorize(Roles = AuthRoles.Admin)]
    public async Task<IActionResult> UpdateOpeningHours(UpdateOpeningHoursCommand command, CancellationToken cancellationToken)
    {
        await _dispatcher.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpPut("delivery-area")]
    [Authorize(Roles = AuthRoles.Admin)]
    public async Task<IActionResult> UpdateDeliveryArea(UpdateDeliveryAreaCommand command, CancellationToken cancellationToken)
    {
        await _dispatcher.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpPut("ordering-thresholds")]
    [Authorize(Roles = AuthRoles.Admin)]
    public async Task<IActionResult> UpdateOrderingThresholds(UpdateOrderingThresholdsCommand command, CancellationToken cancellationToken)
    {
        await _dispatcher.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpPost("accepting-orders")]
    [Authorize(Roles = AuthRoles.Staff)]
    public async Task<IActionResult> ToggleAcceptingOrders(ToggleAcceptingOrdersCommand command, CancellationToken cancellationToken)
    {
        await _dispatcher.Send(command, cancellationToken);
        return NoContent();
    }
}
