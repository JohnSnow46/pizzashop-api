import { apiClient } from './client'
import type { MenuItem } from './types'

export function getMenu(): Promise<MenuItem[]> {
  return apiClient.get<MenuItem[]>('/menu')
}

export function getMenuItemById(id: string): Promise<MenuItem> {
  return apiClient.get<MenuItem>(`/menu/${id}`)
}
