using PizzaShop.Application.Common.Messaging;
using PizzaShop.Application.Reviews.Dtos;

namespace PizzaShop.Application.Reviews.Queries;

/// <summary>Customer role. No parameters — scoped via <c>ICurrentUser</c> in the handler (ADR-0039 pattern).</summary>
public sealed record GetMyReviewsQuery : IQuery<IReadOnlyList<ReviewDto>>;
