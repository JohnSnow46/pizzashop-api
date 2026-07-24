using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PizzaShop.Api.Auth;
using PizzaShop.Application.Common.Abstractions;
using PizzaShop.Application.Common.Messaging;
using PizzaShop.Application.Identity.Commands;
using PizzaShop.Application.Identity.Dtos;
using PizzaShop.Application.Identity.Queries;

namespace PizzaShop.Api.Controllers;

/// <summary>
/// Identity endpoints (api-layer.md 6.1, ADR-0026/0027). Thin: maps request -> Command, calls
/// <see cref="IDispatcher"/>, maps result -> <see cref="IActionResult"/>. No business logic.
/// </summary>
[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IDispatcher _dispatcher;
    private readonly ICurrentUser _currentUser;

    public AuthController(IDispatcher dispatcher, ICurrentUser currentUser)
    {
        _dispatcher = dispatcher;
        _currentUser = currentUser;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResultDto>> Register(RegisterCustomerCommand command, CancellationToken cancellationToken)
    {
        var result = await _dispatcher.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResultDto>> Login(LoginCommand command, CancellationToken cancellationToken)
    {
        var result = await _dispatcher.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Creating a staff account requires at least <see cref="AuthRoles.Admin"/>; the
    /// finer-grained "RestaurantAdmin can only create Employee" rule is state/role-dependent
    /// and enforced in the handler (ADR-0017), not by this attribute.
    /// </summary>
    [HttpPost("staff")]
    [Authorize(Roles = AuthRoles.Admin)]
    public async Task<ActionResult<AuthResultDto>> RegisterStaff(RegisterStaffAccountCommand command, CancellationToken cancellationToken)
    {
        var result = await _dispatcher.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpGet("staff")]
    [Authorize(Roles = AuthRoles.Admin)]
    public async Task<ActionResult<IReadOnlyList<UserAccountDto>>> GetStaff(CancellationToken cancellationToken)
    {
        var result = await _dispatcher.Send(new GetStaffAccountsQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("me")]
    [Authorize]
    public ActionResult<CurrentUserDto> Me() =>
        Ok(new CurrentUserDto(_currentUser.UserAccountId, _currentUser.Role, _currentUser.CustomerId));
}
