using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Application.Common.Abstractions;
using PizzaShop.Application.Common.Exceptions;
using PizzaShop.Application.Common.Messaging;
using PizzaShop.Domain.Catalog;
using PizzaShop.Domain.ValueObjects;

namespace PizzaShop.Application.Catalog.Commands;

public sealed class UpdateIngredientCommandHandler : ICommandHandler<UpdateIngredientCommand>
{
    private readonly IIngredientRepository _ingredientRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateIngredientCommandHandler(IIngredientRepository ingredientRepository, IUnitOfWork unitOfWork)
    {
        _ingredientRepository = ingredientRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(UpdateIngredientCommand command, CancellationToken cancellationToken)
    {
        var ingredient = await _ingredientRepository.GetByIdAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Ingredient), command.Id);

        ingredient.Rename(command.Name);
        ingredient.UpdatePrice(new Money(command.ExtraPrice.Amount, command.ExtraPrice.Currency));

        if (command.IsAvailable)
            ingredient.MarkAvailable();
        else
            ingredient.MarkUnavailable();

        await _ingredientRepository.UpdateAsync(ingredient, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
