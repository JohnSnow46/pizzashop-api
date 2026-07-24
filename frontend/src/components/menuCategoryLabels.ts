import type { MenuCategory } from '../api/types'

/** Polish label per MenuCategory (all 5 values, src/PizzaShop.Domain/Enums/MenuCategory.cs). */
export const CATEGORY_LABELS: Record<MenuCategory, string> = {
  Pizza: 'Pizza',
  Drink: 'Napoje',
  Side: 'Dodatki',
  Dessert: 'Desery',
  Sauce: 'Sosy',
}
