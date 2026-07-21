using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PizzaShop.Api.Auth;
using PizzaShop.Application.Catalog.Commands;
using PizzaShop.Application.Common.Messaging;

namespace PizzaShop.Api.Controllers;

/// <summary>
/// Ingredient dictionary endpoints (api-layer.md 6.3). Thin: maps request -> Command, calls
/// <see cref="IDispatcher"/>, maps result -> <see cref="IActionResult"/>. No business logic.
/// No GET listing endpoint on purpose — there is no corresponding Query in Application yet
/// (api-layer.md 6.3 note); do not add one speculatively.
/// </summary>
[ApiController]
[Route("api/ingredients")]
public sealed class IngredientsController : ControllerBase
{
    private readonly IDispatcher _dispatcher;

    public IngredientsController(IDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    [HttpPost]
    [Authorize(Roles = AuthRoles.Admin)]
    public async Task<ActionResult<Guid>> Create(CreateIngredientCommand command, CancellationToken cancellationToken)
    {
        var id = await _dispatcher.Send(command, cancellationToken);
        return Ok(id);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = AuthRoles.Admin)]
    public async Task<IActionResult> Update(Guid id, UpdateIngredientCommand command, CancellationToken cancellationToken)
    {
        await _dispatcher.Send(command with { Id = id }, cancellationToken);
        return NoContent();
    }
}
