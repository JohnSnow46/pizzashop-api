using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PizzaShop.Api.Auth;
using PizzaShop.Application.Common.Messaging;
using PizzaShop.Application.Reports.Dtos;
using PizzaShop.Application.Reports.Queries;

namespace PizzaShop.Api.Controllers;

/// <summary>
/// Admin reporting endpoints. Thin: maps request -> Query, calls <see cref="IDispatcher"/>,
/// maps result -> <see cref="IActionResult"/>. No business logic.
/// </summary>
[ApiController]
[Route("api/reports")]
[Authorize(Roles = AuthRoles.Admin)]
public sealed class ReportsController : ControllerBase
{
    private readonly IDispatcher _dispatcher;

    public ReportsController(IDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    /// <summary>GET /api/reports/sales — order count, revenue and top-selling menu items for a date range.</summary>
    [HttpGet("sales")]
    public async Task<ActionResult<SalesReportDto>> GetSalesReport(
        [FromQuery] DateTimeOffset from,
        [FromQuery] DateTimeOffset to,
        [FromQuery] int topItems = 5,
        CancellationToken cancellationToken = default)
    {
        var result = await _dispatcher.Send(new GetSalesReportQuery(from, to, topItems), cancellationToken);
        return Ok(result);
    }
}
