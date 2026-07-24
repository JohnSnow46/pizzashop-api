using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Application.Common.Dtos;
using PizzaShop.Application.Common.Messaging;
using PizzaShop.Application.Reports.Dtos;

namespace PizzaShop.Application.Reports.Queries;

public sealed class GetSalesReportQueryHandler : IQueryHandler<GetSalesReportQuery, SalesReportDto>
{
    private readonly IReportingRepository _reportingRepository;

    public GetSalesReportQueryHandler(IReportingRepository reportingRepository)
    {
        _reportingRepository = reportingRepository;
    }

    public async Task<SalesReportDto> Handle(GetSalesReportQuery query, CancellationToken cancellationToken)
    {
        var data = await _reportingRepository.GetSalesReportAsync(query.From, query.To, query.TopItems, cancellationToken);

        var topMenuItems = data.TopMenuItems
            .Select(item => new TopMenuItemDto(
                item.MenuItemId,
                item.MenuItemName,
                item.QuantitySold,
                new MoneyDto(item.Revenue.Amount, item.Revenue.Currency)))
            .ToList();

        return new SalesReportDto(
            query.From,
            query.To,
            data.OrderCount,
            new MoneyDto(data.Revenue.Amount, data.Revenue.Currency),
            topMenuItems);
    }
}
