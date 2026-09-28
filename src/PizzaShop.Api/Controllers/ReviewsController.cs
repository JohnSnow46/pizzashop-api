using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PizzaShop.Api.Auth;
using PizzaShop.Application.Common.Messaging;
using PizzaShop.Application.Reviews.Commands;
using PizzaShop.Application.Reviews.Dtos;
using PizzaShop.Application.Reviews.Queries;

namespace PizzaShop.Api.Controllers;

/// <summary>
/// Review endpoints (ADR-0042). Thin: maps request -> Command/Query, calls
/// <see cref="IDispatcher"/>, maps result -> <see cref="IActionResult"/>. No business logic.
/// </summary>
[ApiController]
public sealed class ReviewsController : ControllerBase
{
    private readonly IDispatcher _dispatcher;

    public ReviewsController(IDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    [HttpPost("api/reviews")]
    [Authorize(Roles = AuthRoles.Customer)]
    public async Task<ActionResult<Guid>> Create(CreateReviewCommand command, CancellationToken cancellationToken)
    {
        var id = await _dispatcher.Send(command, cancellationToken);
        return Ok(id);
    }

    [HttpGet("api/reviews/mine")]
    [Authorize(Roles = AuthRoles.Customer)]
    public async Task<ActionResult<IReadOnlyList<ReviewDto>>> Mine(CancellationToken cancellationToken)
    {
        var result = await _dispatcher.Send(new GetMyReviewsQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("api/menu-items/{menuItemId:guid}/reviews")]
    [AllowAnonymous]
    public async Task<ActionResult<MenuItemReviewsDto>> ForMenuItem(Guid menuItemId, CancellationToken cancellationToken)
    {
        var result = await _dispatcher.Send(new GetMenuItemReviewsQuery(menuItemId), cancellationToken);
        return Ok(result);
    }
}
