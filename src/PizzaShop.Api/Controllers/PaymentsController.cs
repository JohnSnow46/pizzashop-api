using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PizzaShop.Application.Common.Messaging;
using PizzaShop.Application.Payments.Commands;
using PizzaShop.Application.Payments.Dtos;
using PizzaShop.Application.Payments.Queries;

namespace PizzaShop.Api.Controllers;

/// <summary>
/// Payment endpoints (api-layer.md 6.7). Thin: maps request -> Command/Query, calls
/// <see cref="IDispatcher"/>, maps result -> <see cref="IActionResult"/>. No business logic —
/// ownership scoping lives in the handlers (ADR-0017).
/// </summary>
[ApiController]
[Route("api/payments")]
public sealed class PaymentsController : ControllerBase
{
    private readonly IDispatcher _dispatcher;

    public PaymentsController(IDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    [HttpPost("orders/{id:guid}/initialize")]
    [Authorize]
    public async Task<ActionResult<InitializePaymentResultDto>> Initialize(Guid id, CancellationToken cancellationToken)
    {
        var result = await _dispatcher.Send(new InitializePaymentCommand(id), cancellationToken);
        return Ok(result);
    }

    [HttpGet("orders/{id:guid}/status")]
    [Authorize]
    public async Task<ActionResult<PaymentStatusDto>> GetStatus(Guid id, CancellationToken cancellationToken)
    {
        var result = await _dispatcher.Send(new GetPaymentStatusQuery(id), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// PayU calls this, not a logged-in user — <see cref="AllowAnonymousAttribute"/> is the
    /// only correct auth here, guarded instead by signature verification inside
    /// <c>IPaymentGateway.VerifyAndParseNotification</c> (api-layer.md 7, ADR-0013/0022).
    /// Signature verification needs the exact bytes PayU sent, so this action reads the raw
    /// body itself instead of binding a model — <c>[FromBody]</c> deserialization would not
    /// preserve the exact byte sequence the signature was computed over.
    /// </summary>
    [HttpPost("payu/webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> PayUWebhook(CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body, Encoding.UTF8);
        var rawBody = await reader.ReadToEndAsync(cancellationToken);

        var headers = Request.Headers.ToDictionary(
            h => h.Key,
            h => h.Value.ToString(),
            StringComparer.OrdinalIgnoreCase);

        await _dispatcher.Send(new ConfirmPaymentFromNotificationCommand(rawBody, headers), cancellationToken);
        return Ok();
    }
}
