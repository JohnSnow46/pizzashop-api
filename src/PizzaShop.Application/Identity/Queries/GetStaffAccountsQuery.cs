using PizzaShop.Application.Common.Messaging;
using PizzaShop.Application.Identity.Dtos;

namespace PizzaShop.Application.Identity.Queries;

/// <summary>List of staff accounts for the admin management screen — excludes Customer accounts.</summary>
public sealed record GetStaffAccountsQuery : IQuery<IReadOnlyList<UserAccountDto>>;
