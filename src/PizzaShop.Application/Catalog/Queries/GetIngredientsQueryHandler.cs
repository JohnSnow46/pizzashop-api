using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Application.Catalog.Dtos;
using PizzaShop.Application.Common.Messaging;

namespace PizzaShop.Application.Catalog.Queries;

public sealed class GetIngredientsQueryHandler : IQueryHandler<GetIngredientsQuery, IReadOnlyList<IngredientDto>>
{
    private readonly IIngredientRepository _ingredientRepository;

    public GetIngredientsQueryHandler(IIngredientRepository ingredientRepository)
    {
        _ingredientRepository = ingredientRepository;
    }

    public async Task<IReadOnlyList<IngredientDto>> Handle(GetIngredientsQuery query, CancellationToken cancellationToken)
    {
        var ingredients = await _ingredientRepository.GetAllAsync(cancellationToken);
        return ingredients.Select(MenuItemMapper.ToDto).ToList();
    }
}
