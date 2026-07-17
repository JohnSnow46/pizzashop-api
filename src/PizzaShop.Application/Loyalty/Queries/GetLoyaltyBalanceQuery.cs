using PizzaShop.Application.Common.Messaging;
using PizzaShop.Application.Loyalty.Dtos;

namespace PizzaShop.Application.Loyalty.Queries;

/// <summary>
/// Returns the current customer's own loyalty balance and transaction history
/// (application-layer.md 4.6). Scoped to <see cref="Common.Abstractions.ICurrentUser.CustomerId"/> —
/// there is no id parameter, so a request can never ask for someone else's balance.
/// </summary>
public sealed record GetLoyaltyBalanceQuery : IQuery<LoyaltyBalanceDto>;
