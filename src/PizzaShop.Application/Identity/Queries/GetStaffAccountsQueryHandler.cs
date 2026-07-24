using PizzaShop.Application.Common.Abstractions;
using PizzaShop.Application.Common.Messaging;
using PizzaShop.Application.Identity.Abstractions;
using PizzaShop.Application.Identity.Dtos;

namespace PizzaShop.Application.Identity.Queries;

public sealed class GetStaffAccountsQueryHandler : IQueryHandler<GetStaffAccountsQuery, IReadOnlyList<UserAccountDto>>
{
    private readonly IUserAccountRepository _userAccountRepository;

    public GetStaffAccountsQueryHandler(IUserAccountRepository userAccountRepository)
    {
        _userAccountRepository = userAccountRepository;
    }

    public async Task<IReadOnlyList<UserAccountDto>> Handle(GetStaffAccountsQuery query, CancellationToken cancellationToken)
    {
        var accounts = await _userAccountRepository.GetAllAsync(cancellationToken);

        return accounts
            .Where(a => a.Role != UserRole.Customer)
            .Select(a => new UserAccountDto(a.Id, a.Email, a.Role, a.IsActive, a.CreatedAt))
            .ToList();
    }
}
