using PizzaShop.Application.Common.Messaging;
using PizzaShop.Application.Payments.Dtos;

namespace PizzaShop.Application.Payments.Commands;

/// <summary>
/// Guest counterpart of <see cref="InitializePaymentCommand"/> (application-layer.md 4.4,
/// ADR-0041): (re-)initializes an online payment session for a guest order looked up by its
/// unguessable <see cref="GuestTrackingToken"/> instead of a JWT-bound identity — possession of
/// the token is the access control, exactly like <see cref="Orders.Queries.GetOrderByTrackingTokenQuery"/>.
/// </summary>
public sealed record InitializeGuestPaymentCommand(Guid GuestTrackingToken) : ICommand<InitializePaymentResultDto>;
