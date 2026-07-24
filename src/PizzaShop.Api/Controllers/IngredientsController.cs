using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PizzaShop.Api.Auth;
using PizzaShop.Application.Catalog.Commands;
using PizzaShop.Application.Catalog.Dtos;
using PizzaShop.Application.Catalog.Queries;
using PizzaShop.Application.Common.Messaging;

namespace PizzaShop.Api.Controllers;

/// <summary>
/// Ingredient dictionary endpoints (api-layer.md 6.3). Thin: maps request -> Command/Query,
/// calls <see cref="IDispatcher"/>, maps result -> <see cref="IActionResult"/>. No business logic.
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

    [HttpGet]
    [Authorize(Roles = AuthRoles.Admin)]
    public async Task<ActionResult<IReadOnlyList<IngredientDto>>> GetAll(CancellationToken cancellationToken)
    {
        var result = await _dispatcher.Send(new GetIngredientsQuery(), cancellationToken);
        return Ok(result);
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
