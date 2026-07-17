using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Application.Common.Abstractions;
using PizzaShop.Application.Common.Exceptions;
using PizzaShop.Application.Common.Messaging;
using PizzaShop.Application.Loyalty.Dtos;
using PizzaShop.Domain.Loyalty;

namespace PizzaShop.Application.Loyalty.Queries;

public sealed class GetLoyaltyBalanceQueryHandler : IQueryHandler<GetLoyaltyBalanceQuery, LoyaltyBalanceDto>
{
    private readonly ILoyaltyAccountRepository _loyaltyAccountRepository;
    private readonly ICurrentUser _currentUser;

    public GetLoyaltyBalanceQueryHandler(ILoyaltyAccountRepository loyaltyAccountRepository, ICurrentUser currentUser)
    {
        _loyaltyAccountRepository = loyaltyAccountRepository;
        _currentUser = currentUser;
    }

    public async Task<LoyaltyBalanceDto> Handle(GetLoyaltyBalanceQuery query, CancellationToken cancellationToken)
    {
        var customerId = _currentUser.CustomerId
            ?? throw new ForbiddenOperationException("Only registered customers have a loyalty balance.");

        var account = await _loyaltyAccountRepository.GetByCustomerIdAsync(customerId, cancellationToken)
            ?? throw new NotFoundException(nameof(LoyaltyAccount), customerId);

        return LoyaltyMapper.ToDto(account);
    }
}
