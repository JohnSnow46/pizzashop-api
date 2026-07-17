using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Application.Common.Abstractions;
using PizzaShop.Application.Common.Messaging;
using PizzaShop.Domain.Catalog;
using PizzaShop.Domain.ValueObjects;

namespace PizzaShop.Application.Catalog.Commands;

public sealed class CreateIngredientCommandHandler : ICommandHandler<CreateIngredientCommand, Guid>
{
    private readonly IIngredientRepository _ingredientRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateIngredientCommandHandler(IIngredientRepository ingredientRepository, IUnitOfWork unitOfWork)
    {
        _ingredientRepository = ingredientRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateIngredientCommand command, CancellationToken cancellationToken)
    {
        var ingredient = Ingredient.Create(
            command.Name,
            new Money(command.ExtraPrice.Amount, command.ExtraPrice.Currency),
            command.Category);

        await _ingredientRepository.AddAsync(ingredient, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ingredient.Id;
    }
}
