using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PizzaShop.Api.Auth;
using PizzaShop.Application.Catalog.Commands;
using PizzaShop.Application.Catalog.Dtos;
using PizzaShop.Application.Catalog.Queries;
using PizzaShop.Application.Common.Messaging;

namespace PizzaShop.Api.Controllers;

/// <summary>
/// Catalog/menu endpoints (api-layer.md 6.2). Thin: maps request -> Command/Query, calls
/// <see cref="IDispatcher"/>, maps result -> <see cref="IActionResult"/>. No business logic.
/// </summary>
[ApiController]
[Route("api/menu")]
public sealed class MenuController : ControllerBase
{
    private readonly IDispatcher _dispatcher;

    public MenuController(IDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<MenuItemDto>>> GetMenu(CancellationToken cancellationToken)
    {
        var result = await _dispatcher.Send(new GetMenuQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<MenuItemDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _dispatcher.Send(new GetMenuItemByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = AuthRoles.Admin)]
    public async Task<ActionResult<Guid>> Create(CreateMenuItemCommand command, CancellationToken cancellationToken)
    {
        var id = await _dispatcher.Send(command, cancellationToken);
        return Ok(id);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = AuthRoles.Admin)]
    public async Task<IActionResult> Update(Guid id, UpdateMenuItemCommand command, CancellationToken cancellationToken)
    {
        await _dispatcher.Send(command with { Id = id }, cancellationToken);
        return NoContent();
    }

    [HttpPatch("{id:guid}/availability")]
    [Authorize(Roles = AuthRoles.Staff)]
    public async Task<IActionResult> SetAvailability(Guid id, SetMenuItemAvailabilityCommand command, CancellationToken cancellationToken)
    {
        await _dispatcher.Send(command with { MenuItemId = id }, cancellationToken);
        return NoContent();
    }
}
