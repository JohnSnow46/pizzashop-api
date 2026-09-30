using PizzaShop.Application.Carts.Dtos;
using PizzaShop.Application.Common.Messaging;

namespace PizzaShop.Application.Carts.Queries;

/// <summary>Customer role. No parameters — scoped via <c>ICurrentUser</c>. A customer with no
/// cart yet gets an empty <see cref="CartDto"/>, not a 404 (ADR-0043 §D).</summary>
public sealed record GetMyCartQuery : IQuery<CartDto>;
