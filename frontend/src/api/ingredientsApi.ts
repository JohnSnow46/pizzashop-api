import { apiClient } from './client'
import type { CreateIngredientCommand, Ingredient, UpdateIngredientCommand } from './types'

/** GET /api/ingredients — full ingredient dictionary, for admin catalog management (Admin role). */
export function getIngredients(): Promise<Ingredient[]> {
  return apiClient.get<Ingredient[]>('/ingredients')
}

/** POST /api/ingredients — creates a new ingredient (Admin role). Returns the new ingredient's id. */
export function createIngredient(command: CreateIngredientCommand): Promise<string> {
  return apiClient.post<string>('/ingredients', command)
}

/** PUT /api/ingredients/{id} — updates an existing ingredient (Admin role). */
export function updateIngredient(id: string, command: Omit<UpdateIngredientCommand, 'id'>): Promise<void> {
  return apiClient.put<void>(`/ingredients/${id}`, { ...command, id })
}
