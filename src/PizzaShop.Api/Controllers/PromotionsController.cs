using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PizzaShop.Api.Auth;
using PizzaShop.Application.Common.Messaging;
using PizzaShop.Application.Promotions.Commands;
using PizzaShop.Application.Promotions.Dtos;
using PizzaShop.Application.Promotions.Queries;

namespace PizzaShop.Api.Controllers;

/// <summary>
/// Promotion endpoints (api-layer.md 6.5). Thin: maps request -> Command/Query, calls
/// <see cref="IDispatcher"/>, maps result -> <see cref="IActionResult"/>. No business logic.
/// </summary>
[ApiController]
[Route("api/promotions")]
public sealed class PromotionsController : ControllerBase
{
    private readonly IDispatcher _dispatcher;

    public PromotionsController(IDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    /// <summary>
    /// Preview of the discount a coupon code would apply to the given cart, without applying
    /// it. Exposed as POST (not GET) because the request carries a cart shape (subtotal,
    /// delivery fee) in its body (api-layer.md 6.5).
    /// </summary>
    [HttpPost("validate")]
    [AllowAnonymous]
    public async Task<ActionResult<PromotionDiscountPreviewDto>> Validate(ValidatePromotionCodeQuery query, CancellationToken cancellationToken)
    {
        var result = await _dispatcher.Send(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet]
    [Authorize(Roles = AuthRoles.Admin)]
    public async Task<ActionResult<IReadOnlyList<PromotionDto>>> GetAll(CancellationToken cancellationToken)
    {
        var result = await _dispatcher.Send(new GetPromotionsQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = AuthRoles.Admin)]
    public async Task<ActionResult<Guid>> Create(CreatePromotionCommand command, CancellationToken cancellationToken)
    {
        var id = await _dispatcher.Send(command, cancellationToken);
        return Ok(id);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = AuthRoles.Admin)]
    public async Task<IActionResult> Update(Guid id, UpdatePromotionCommand command, CancellationToken cancellationToken)
    {
        await _dispatcher.Send(command with { PromotionId = id }, cancellationToken);
        return NoContent();
    }

    [HttpPatch("{id:guid}/deactivate")]
    [Authorize(Roles = AuthRoles.Admin)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        await _dispatcher.Send(new DeactivatePromotionCommand(id), cancellationToken);
        return NoContent();
    }
}
