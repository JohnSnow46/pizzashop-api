using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PizzaShop.Api.Auth;
using PizzaShop.Application.Common.Messaging;
using PizzaShop.Application.Orders.Commands;
using PizzaShop.Application.Orders.Dtos;
using PizzaShop.Application.Orders.Queries;

namespace PizzaShop.Api.Controllers;

/// <summary>
/// Order endpoints (api-layer.md 6.6). Thin: maps request -> Command/Query, calls
/// <see cref="IDispatcher"/>, maps result -> <see cref="IActionResult"/>. No business logic —
/// ownership scoping ("own order" vs. staff) and the status-transition graph both live in the
/// handlers/Domain (ADR-0017), not here.
/// </summary>
[ApiController]
[Route("api/orders")]
public sealed class OrdersController : ControllerBase
{
    private readonly IDispatcher _dispatcher;

    public OrdersController(IDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    [HttpPost("check-delivery")]
    [AllowAnonymous]
    public async Task<ActionResult<DeliveryAvailabilityDto>> CheckDelivery(CheckDeliveryAvailabilityQuery query, CancellationToken cancellationToken)
    {
        var result = await _dispatcher.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Places an order as a guest or a logged-in customer — <c>ICurrentUser.CustomerId</c>
    /// (read by the handler, not this controller) decides which, so a request body can never
    /// spoof it (CreateOrderCommand summary).
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    public async Task<ActionResult<CreateOrderResultDto>> Create(CreateOrderCommand command, CancellationToken cancellationToken)
    {
        var result = await _dispatcher.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [Authorize]
    public async Task<ActionResult<OrderDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _dispatcher.Send(new GetOrderByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    [HttpGet("track/{trackingToken:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<OrderDto>> GetByTrackingToken(Guid trackingToken, CancellationToken cancellationToken)
    {
        var result = await _dispatcher.Send(new GetOrderByTrackingTokenQuery(trackingToken), cancellationToken);
        return Ok(result);
    }

    [HttpGet("queue")]
    [Authorize(Roles = AuthRoles.Staff)]
    public async Task<ActionResult<IReadOnlyList<OrderDto>>> GetQueue(CancellationToken cancellationToken)
    {
        var result = await _dispatcher.Send(new GetOrderQueueQuery(), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// <c>AcceptOrderCommand</c> carries an optional <c>EstimatedReadyAt</c> alongside
    /// <c>OrderId</c> — route id wins over the body's (api-layer.md 1.1/ADR-0030).
    /// </summary>
    [HttpPost("{id:guid}/accept")]
    [Authorize(Roles = AuthRoles.Staff)]
    public async Task<IActionResult> Accept(Guid id, AcceptOrderCommand command, CancellationToken cancellationToken)
    {
        await _dispatcher.Send(command with { OrderId = id }, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/reject")]
    [Authorize(Roles = AuthRoles.Staff)]
    public async Task<IActionResult> Reject(Guid id, CancellationToken cancellationToken)
    {
        await _dispatcher.Send(new RejectOrderCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/start-preparation")]
    [Authorize(Roles = AuthRoles.Staff)]
    public async Task<IActionResult> StartPreparation(Guid id, CancellationToken cancellationToken)
    {
        await _dispatcher.Send(new StartPreparationCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/mark-ready")]
    [Authorize(Roles = AuthRoles.Staff)]
    public async Task<IActionResult> MarkReady(Guid id, CancellationToken cancellationToken)
    {
        await _dispatcher.Send(new MarkReadyCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/start-delivery")]
    [Authorize(Roles = AuthRoles.Staff)]
    public async Task<IActionResult> StartDelivery(Guid id, CancellationToken cancellationToken)
    {
        await _dispatcher.Send(new StartDeliveryCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/complete")]
    [Authorize(Roles = AuthRoles.Staff)]
    public async Task<IActionResult> Complete(Guid id, CancellationToken cancellationToken)
    {
        await _dispatcher.Send(new CompleteOrderCommand(id), cancellationToken);
        return NoContent();
    }

    /// <summary>Route id wins over the body's <c>OrderId</c> (api-layer.md 1.1/ADR-0030).</summary>
    [HttpPut("{id:guid}/estimated-ready-at")]
    [Authorize(Roles = AuthRoles.Staff)]
    public async Task<IActionResult> SetEstimatedReadyAt(Guid id, SetEstimatedReadyAtCommand command, CancellationToken cancellationToken)
    {
        await _dispatcher.Send(command with { OrderId = id }, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Any authenticated role may call this — whether a customer may still cancel (only
    /// before <c>Accepted</c>) vs. staff (any time) is a state-dependent rule enforced in
    /// <c>CancelOrderCommandHandler</c>, not an <c>[Authorize]</c> role restriction here
    /// (api-layer.md 6.6).
    /// </summary>
    [HttpPost("{id:guid}/cancel")]
    [Authorize]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        await _dispatcher.Send(new CancelOrderCommand(id), cancellationToken);
        return NoContent();
    }
}
