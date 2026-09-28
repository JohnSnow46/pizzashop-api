using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Application.Common.Abstractions;
using PizzaShop.Application.Common.Exceptions;
using PizzaShop.Application.Common.Messaging;
using PizzaShop.Domain.Enums;
using PizzaShop.Domain.Reviews;

namespace PizzaShop.Application.Reviews.Commands;

/// <summary>
/// Eligibility checks (ADR-0042 §D) live here, not in Domain: they read a second aggregate
/// (<see cref="Domain.Orders.Order"/>), which <see cref="Review"/> deliberately never
/// references directly.
/// </summary>
public sealed class CreateReviewCommandHandler : ICommandHandler<CreateReviewCommand, Guid>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IReviewRepository _reviewRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;

    public CreateReviewCommandHandler(
        IOrderRepository orderRepository,
        IReviewRepository reviewRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IClock clock)
    {
        _orderRepository = orderRepository;
        _reviewRepository = reviewRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Guid> Handle(CreateReviewCommand command, CancellationToken cancellationToken)
    {
        var customerId = _currentUser.CustomerId
            ?? throw new ForbiddenOperationException("Only registered customers can leave reviews.");

        var order = await _orderRepository.GetByIdAsync(command.OrderId, cancellationToken);

        // Same 404-not-403 choice as GetOrderByIdQuery/CancelOrderCommandHandler
        // (ADR-0017/ADR-0039): don't reveal that an order id belongs to someone else.
        if (order is null || order.CustomerId != customerId)
            throw new NotFoundException(nameof(Domain.Orders.Order), command.OrderId);

        if (order.Status != OrderStatus.Completed)
            throw new ConflictException("Order is not yet completed.");

        if (!order.Items.Any(i => i.MenuItemId == command.MenuItemId))
            throw new ConflictException("This menu item was not part of the order.");

        var existing = await _reviewRepository.GetByOrderAndMenuItemAsync(command.OrderId, command.MenuItemId, cancellationToken);
        if (existing is not null)
            throw new ConflictException("This item has already been reviewed.");

        var review = Review.Create(
            command.OrderId,
            customerId,
            command.MenuItemId,
            command.Rating,
            command.Comment,
            _clock.UtcNow);

        await _reviewRepository.AddAsync(review, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return review.Id;
    }
}
