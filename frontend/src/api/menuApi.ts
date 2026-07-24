import { apiClient } from './client'
import type { CreateMenuItemCommand, MenuItem, UpdateMenuItemCommand } from './types'

export function getMenu(): Promise<MenuItem[]> {
  return apiClient.get<MenuItem[]>('/menu')
}

export function getMenuItemById(id: string): Promise<MenuItem> {
  return apiClient.get<MenuItem>(`/menu/${id}`)
}

/** POST /api/menu — creates a new menu item (Admin role). Returns the new item's id. */
export function createMenuItem(command: CreateMenuItemCommand): Promise<string> {
  return apiClient.post<string>('/menu', command)
}

/**
 * PUT /api/menu/{id} — full-replace update of a menu item, including base ingredients,
 * allowed extras and variant reconciliation (ADR-0016). Admin role.
 */
export function updateMenuItem(id: string, command: Omit<UpdateMenuItemCommand, 'id'>): Promise<void> {
  return apiClient.put<void>(`/menu/${id}`, { ...command, id })
}

/** PATCH /api/menu/{id}/availability — toggles a menu item's availability (Staff role). */
export function setMenuItemAvailability(id: string, isAvailable: boolean): Promise<void> {
  return apiClient.patch<void>(`/menu/${id}/availability`, { menuItemId: id, isAvailable })
}
